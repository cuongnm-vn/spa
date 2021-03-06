﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SHCServer.Models;
using SHCServer.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Viettel;
using Viettel.MySql;

namespace SHCServer.Controllers
{
    public class DoctorController : BaseController
    {
        private readonly string _connectionString;
        private string nameData;
        public DoctorController(IOptions<Audience> settings, IConfiguration configuration)
        {
            _settings = settings;
            _context = new MySqlContext(new MySqlConnectionFactory(configuration.GetConnectionString("DefaultConnection")));
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _excep = new FriendlyException();

            if (_connectionString.IndexOf("smarthealthcare_58") > 0) nameData = "smarthealthcare_58";
            else if (_connectionString.IndexOf("smarthealthcare") > 0) nameData = "smarthealthcare";
        }

        [HttpGet]
        [Route("api/doctor")]
        public IActionResult GetAll(int skipCount = 0, int maxResultCount = 10, string sorting = null, string filter = null)
        {
            var objs = _context.Query<Doctor>().Where(d => d.IsDelete == false && d.IsActive == true)
                .LeftJoin<HealthFacilitiesDoctors>((d, hf) => d.DoctorId == hf.DoctorId && hf.IsDelete == false && hf.IsActive == true)
                .LeftJoin<DoctorSpecialists>((d, hf, ds) => d.DoctorId == ds.DoctorId && ds.IsDelete == false && ds.IsActive == true)
                .Select((d, hf, ds) => new
                {
                    d.AcademicId,
                    d.Address,
                    d.AllowBooking,
                    d.AllowFilter,
                    d.AllowSearch,
                    d.Avatar,

                    d.BirthDate,
                    d.BirthMonth,
                    d.BirthYear,

                    d.CertificationCode,
                    d.CertificationDate,
                    d.CreateDate,

                    d.DegreeId,
                    d.Description,
                    d.DistrictCode,
                    d.DoctorId,

                    d.EducationCountryCode,
                    d.Email,
                    d.EthnicityCode,

                    d.FullName,

                    d.Gender,

                    d.HisId,

                    d.IsActive,
                    d.IsDelete,
                    d.IsSync,
                    d.NationCode,
                    d.PhoneNumber,
                    d.PositionCode,
                    d.PriceDescription,
                    d.PriceFrom,
                    d.PriceTo,
                    d.ProvinceCode,
                    d.Summary,
                    d.TitleCode,

                    hf.HealthFacilitiesId,
                    ds.SpecialistCode,
                });
            if (filter != null)
            {
                foreach (var (key, value) in JsonConvert.DeserializeObject<Dictionary<string, string>>(filter))
                {
                    if (string.IsNullOrEmpty(value.Trim()))
                        continue;

                    if (string.Equals(key, "provinceCode"))
                    {
                        objs = objs.Where(d => d.ProvinceCode.ToString().Equals(value.Trim()));
                    }
                    if (string.Equals(key, "districtCode"))
                    {
                        objs = objs.Where(d => d.DistrictCode.ToString().Equals(value.Trim()));
                    }
                    if (string.Equals(key, "healthfacilitiesId"))
                    {
                        objs = objs.Where(d => d.HealthFacilitiesId.ToString().Equals(value.Trim()));
                    }
                    if (string.Equals(key, "specialistCode"))
                    {
                        objs = objs.Where(d => d.SpecialistCode.ToString().Equals(value.Trim()));
                    }
                    if (string.Equals(key, "info") && !string.IsNullOrEmpty(value))
                    {
                        var s = value.Replace(@"%", "\\%").Replace(@"_", "\\_").Trim();
                        objs = objs.Where(d => d.FullName.ToString().Contains(s) || (d.PhoneNumber.Equals(s)));
                    }
                }
            }

            objs = objs.GroupBy(o => o.DoctorId).Select((d) => new
            {
                d.AcademicId,
                d.Address,
                d.AllowBooking,
                d.AllowFilter,
                d.AllowSearch,
                d.Avatar,

                d.BirthDate,
                d.BirthMonth,
                d.BirthYear,

                d.CertificationCode,
                d.CertificationDate,
                d.CreateDate,

                d.DegreeId,
                d.Description,
                d.DistrictCode,
                d.DoctorId,

                d.EducationCountryCode,
                d.Email,
                d.EthnicityCode,

                d.FullName,

                d.Gender,

                d.HisId,

                d.IsActive,
                d.IsDelete,
                d.IsSync,
                d.NationCode,
                d.PhoneNumber,
                d.PositionCode,
                d.PriceDescription,
                d.PriceFrom,
                d.PriceTo,
                d.ProvinceCode,
                d.Summary,
                d.TitleCode,

                d.HealthFacilitiesId,
                d.SpecialistCode,
            });

            if (sorting != null)
            {
                foreach (var (key, value) in JsonConvert.DeserializeObject<Dictionary<string, string>>(sorting))
                {
                    //if (!Utils.PropertyExists<Doctor>(key))
                    //    continue;
                    if (string.Equals(key, "id"))
                    {
                        objs = value == "asc" ? objs.OrderBy(u => u.CreateDate) : objs.OrderByDesc(u => u.CreateDate);
                    }
                    else
                    {
                        objs = value == "asc" ? objs.OrderBy(u => u.FullName) : objs.OrderByDesc(u => u.FullName);
                    }
                }
            }
            else
            {
                objs = objs.OrderByDesc(d => d.CreateDate).ThenByDesc(d => d.FullName);
            }

            List<DoctorViewModel> doctorList = new List<DoctorViewModel>();

            var total = objs.Count();

            objs = objs.TakePage(skipCount == 0 ? 1 : skipCount + 1, maxResultCount);

            foreach (var item in objs.ToList())
            {
                doctorList.Add(new DoctorViewModel(new Doctor()
                {
                    AcademicId = item.AcademicId,
                    Address = item.Address,
                    AllowBooking = item.AllowBooking,
                    AllowFilter = item.AllowFilter,
                    AllowSearch = item.AllowSearch,
                    Avatar = item.Avatar,

                    BirthDate = item.BirthDate,
                    BirthMonth = item.BirthMonth,
                    BirthYear = item.BirthYear,

                    CertificationCode = item.CertificationCode,
                    CertificationDate = item.CertificationDate,
                    CreateDate = item.CreateDate,

                    DegreeId = item.DegreeId,
                    Description = item.Description,
                    DistrictCode = item.DistrictCode,
                    DoctorId = item.DoctorId,

                    EducationCountryCode = item.EducationCountryCode,
                    Email = item.Email,
                    EthnicityCode = item.EthnicityCode,

                    FullName = item.FullName,

                    Gender = item.Gender,

                    HisId = item.HisId,

                    IsActive = item.IsActive,
                    IsDelete = item.IsDelete,
                    IsSync = item.IsSync,
                    NationCode = item.NationCode,
                    PhoneNumber = item.PhoneNumber,
                    PositionCode = item.PositionCode,
                    PriceDescription = item.PriceDescription,
                    PriceFrom = item.PriceFrom,
                    PriceTo = item.PriceTo,
                    ProvinceCode = item.ProvinceCode,
                    Summary = item.Summary,
                    TitleCode = item.TitleCode,
                }, _connectionString));
            }

            //return Json(new ActionResultDto { Result = new { Items = objs.TakePage(skipCount == 0 ? 1 : skipCount + 1, maxResultCount).ToList(), TotalCount = objs.Count() } });
            return Json(new ActionResultDto { Result = new { Items = doctorList, TotalCount = total } });
        }

        #region Old_Code

        [HttpPost]
        [Route("api/doctor")]
        public IActionResult Create([FromForm] DoctorInputViewModel doctor)
        {
            bool checkCertificationDate = true;

            if (!string.IsNullOrEmpty(doctor.CertificationCode))
            {
                var checkCertification = _context.Query<Doctor>().Where(d => d.CertificationCode.Equals(doctor.CertificationCode)).FirstOrDefault();

                if (checkCertification != null)
                {
                    return StatusCode(406, _excep.Throw(406, "Lưu không thành công", "Số giấy phép hành nghề đã tồn tại", ""));
                }
            }
            try
            {
                DateTime.Parse(doctor.CertificationDate);
                if (doctor.CertificationDate == null)
                    checkCertificationDate = false;
            }
            catch
            {
                checkCertificationDate = false;
            }

            if (!string.IsNullOrEmpty(doctor.EthnicityCode))
                if (doctor.EthnicityCode.Equals("null"))
                {
                    doctor.EthnicityCode = "";
                }
            if (!string.IsNullOrEmpty(doctor.EducationCountryCode))
                if (doctor.EducationCountryCode.Equals("null"))
                {
                    doctor.EducationCountryCode = "";
                }
            if (!string.IsNullOrEmpty(doctor.NationCode))
                if (doctor.NationCode.Equals("null"))
                {
                    doctor.NationCode = "";
                }
            if (!string.IsNullOrEmpty(doctor.Address))
                if (doctor.Address.Equals("null"))
                {
                    doctor.Address = "";
                }
            if (!string.IsNullOrEmpty(doctor.PhoneNumber))
                if (doctor.PhoneNumber.Equals("null"))
                {
                    doctor.PhoneNumber = "";
                }
            if (!string.IsNullOrEmpty(doctor.Email))
                if (doctor.Email.Equals("null"))
                {
                    doctor.Email = "";
                }
            if (!string.IsNullOrEmpty(doctor.CertificationCode))
                if (doctor.CertificationCode.Equals("null"))
                {
                    doctor.CertificationCode = "";
                }
            if (!string.IsNullOrEmpty(doctor.Summary))
                if (doctor.Summary.Equals("null"))
                {
                    doctor.Summary = "";
                }
            if (!string.IsNullOrEmpty(doctor.PriceDescription))
                if (doctor.PriceDescription.Equals("null"))
                {
                    doctor.PriceDescription = "";
                }
            if (!string.IsNullOrEmpty(doctor.Description))
                if (doctor.Description.Equals("null"))
                {
                    doctor.Description = "";
                }
            if (doctor.Summary != null)
                if (doctor.Summary.Length > 4000)
                {
                    doctor.Summary = doctor.Summary.Substring(0, 4000);
                }

            try
            {
                var _files = Request.Form.Files;
                var _fileUpload = "";
                if (_files.Count > 0)
                {
                    foreach (var file in _files)
                    {
                        var uniqueFileName = GetUniqueFileName(file.FileName);
                        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        var filePath = Path.Combine(uploads, uniqueFileName);
                        _fileUpload = "/uploads/" + uniqueFileName;
                        file.CopyTo(new FileStream(filePath, FileMode.Create));
                    }
                }

                _context.Session.BeginTransaction();

                _context.Insert(() => new Doctor()
                {
                    AcademicId = doctor.AcademicId,
                    Address = doctor.Address,
                    AllowBooking = doctor.AllowBooking,
                    AllowFilter = doctor.AllowFilter,
                    AllowSearch = doctor.AllowSearch,
                    Avatar = _fileUpload,
                    BirthDate = doctor.BirthDate,
                    BirthMonth = doctor.BirthMonth,
                    BirthYear = doctor.BirthYear,
                    CertificationCode = doctor.CertificationCode,
                    CreateUserId = doctor.CreateUserId,
                    CreateDate = DateTime.Now,
                    DegreeId = doctor.DegreeId,
                    Description = doctor.Description,
                    DistrictCode = doctor.DistrictCode,
                    EducationCountryCode = doctor.EducationCountryCode,
                    Email = doctor.Email,
                    EthnicityCode = doctor.EthnicityCode,
                    FullName = doctor.FullName,
                    Gender = doctor.Gender,
                    HisId = doctor.HisId,
                    NationCode = doctor.NationCode,
                    PhoneNumber = doctor.PhoneNumber,
                    PositionCode = doctor.PositionCode,
                    PriceDescription = doctor.PriceDescription,
                    PriceFrom = doctor.PriceFrom,
                    PriceTo = doctor.PriceTo,
                    ProvinceCode = doctor.ProvinceCode,
                    Summary = doctor.Summary,
                    UpdateDate = DateTime.Now,
                    UpdateUserId = doctor.UpdateUserId,
                    TitleCode = doctor.TitleCode,
                });

                _context.Session.CommitTransaction();

                var newDoctor = _context.Query<Doctor>().Where(d => d.IsDelete == false && d.IsActive == true).OrderByDesc(d => d.CreateDate).FirstOrDefault();

                _context.Session.BeginTransaction();

                //if (doctor.Specialist.Count != 0)
                //{
                //    foreach (var item in doctor.Specialist)
                //    {
                //        _context.Insert(() => new DoctorSpecialists()
                //        {
                //            DoctorId = newDoctor.DoctorId,
                //            SpecialistCode = item.SpecialistCode
                //        });
                //    }
                //}
                string[] specialist;
                if (!string.IsNullOrEmpty(doctor.Specials))
                {
                    specialist = doctor.Specials.Split(',');
                    foreach (var item in specialist)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            _context.Insert(() => new DoctorSpecialists()
                            {
                                DoctorId = newDoctor.DoctorId,
                                SpecialistCode = item
                            });
                        }
                    }
                }

                string[] healhs;

                if (!string.IsNullOrEmpty(doctor.Healths))
                {
                    healhs = doctor.Healths.Split(',');
                    foreach (var item in healhs)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            _context.Insert(() => new HealthFacilitiesDoctors()
                            {
                                DoctorId = newDoctor.DoctorId,
                                HealthFacilitiesId = Convert.ToInt32(item)
                            });
                        }
                    }
                }

                if (checkCertificationDate == true)
                {
                    var date = DateTime.Parse(doctor.CertificationDate);
                    _context.Update<Doctor>(d => d.DoctorId == newDoctor.DoctorId, x => new Doctor()
                    {
                        CertificationDate = date
                    });
                }

                //if (doctor.HealthFacilities.Count != 0)
                //{
                //    foreach (var item in doctor.HealthFacilities)
                //    {
                //        _context.Insert(() => new HealthFacilitiesDoctors()
                //        {
                //            HealthFacilitiesId = item.HealthFacilitiesId,
                //            DoctorId = newDoctor.DoctorId
                //        });
                //    }
                //}

                _context.Session.CommitTransaction();

                return Json(new ActionResultDto());
            }
            catch (Exception e)
            {
                if (_context.Session.IsInTransaction)
                    _context.Session.RollbackTransaction();
                return StatusCode(500, _excep.Throw("Có lỗi xảy ra !", e.Message));
            }
        }

        [HttpPut]
        [Route("api/doctor")]
        public IActionResult Update([FromForm] DoctorInputViewModel doctor, int? allow)
        {
            bool checkCertificationDate = true;
            if (allow != 1)
            {
                if (!string.IsNullOrEmpty(doctor.CertificationCode))
                {
                    var checkCertification = _context.Query<Doctor>().Where(d => d.CertificationCode.Equals(doctor.CertificationCode)).FirstOrDefault();

                    if (checkCertification != null && checkCertification.DoctorId != doctor.DoctorId)
                    {
                        return StatusCode(406, _excep.Throw(406, "Lưu không thành công", "Số giấy phép hành nghề đã tồn tại", ""));
                    }
                }
            }

            try
            {
                DateTime.Parse(doctor.CertificationDate);
                if (doctor.CertificationDate == null)
                    checkCertificationDate = false;
            }
            catch
            {
                checkCertificationDate = false;
            }

            if (!string.IsNullOrEmpty(doctor.EthnicityCode))
                if (doctor.EthnicityCode.Equals("null"))
                {
                    doctor.EthnicityCode = "";
                }
            if (!string.IsNullOrEmpty(doctor.EducationCountryCode))
                if (doctor.EducationCountryCode.Equals("null"))
                {
                    doctor.EducationCountryCode = "";
                }
            if (!string.IsNullOrEmpty(doctor.NationCode))
                if (doctor.NationCode.Equals("null"))
                {
                    doctor.NationCode = "";
                }
            if (!string.IsNullOrEmpty(doctor.Address))
                if (doctor.Address.Equals("null"))
                {
                    doctor.Address = "";
                }
            if (!string.IsNullOrEmpty(doctor.PhoneNumber))
                if (doctor.PhoneNumber.Equals("null"))
                {
                    doctor.PhoneNumber = "";
                }
            if (!string.IsNullOrEmpty(doctor.Email))
                if (doctor.Email.Equals("null"))
                {
                    doctor.Email = "";
                }
            if (!string.IsNullOrEmpty(doctor.CertificationCode))
                if (doctor.CertificationCode.Equals("null"))
                {
                    doctor.CertificationCode = "";
                }
            if (!string.IsNullOrEmpty(doctor.Summary))
                if (doctor.Summary.Equals("null"))
                {
                    doctor.Summary = "";
                }
            if (!string.IsNullOrEmpty(doctor.PriceDescription))
                if (doctor.PriceDescription.Equals("null"))
                {
                    doctor.PriceDescription = "";
                }
            if (!string.IsNullOrEmpty(doctor.Description))
                if (doctor.Description.Equals("null"))
                {
                    doctor.Description = "";
                }
            if (doctor.Summary != null)
                if (doctor.Summary.Length > 4000)
                {
                    doctor.Summary = doctor.Summary.Substring(0, 4000);
                }

            var checkDoctor = _context.Query<Doctor>().Where(d => d.DoctorId == doctor.DoctorId).FirstOrDefault();

            if (checkDoctor == null)
            {
                return StatusCode(404, _excep.Throw("Not Found"));
            }
            try
            {
                var _files = Request.Form.Files;
                var _fileUpload = "";
                if (_files.Count > 0)
                {
                    foreach (var file in _files)
                    {
                        var uniqueFileName = GetUniqueFileName(file.FileName);
                        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        var filePath = Path.Combine(uploads, uniqueFileName);
                        _fileUpload = "/uploads/" + uniqueFileName;
                        file.CopyTo(new FileStream(filePath, FileMode.Create));
                    }
                }

                _context.BeginTransaction();

                _context.Update<Doctor>(d => d.DoctorId == doctor.DoctorId, x => new Doctor()
                {
                    AcademicId = doctor.AcademicId,
                    Address = doctor.Address,
                    AllowBooking = doctor.AllowBooking,
                    AllowFilter = doctor.AllowFilter,
                    AllowSearch = doctor.AllowSearch,
                    BirthDate = doctor.BirthDate,
                    BirthMonth = doctor.BirthMonth,
                    BirthYear = doctor.BirthYear,
                    CertificationCode = doctor.CertificationCode,
                    DegreeId = doctor.DegreeId,
                    Description = doctor.Description,
                    DistrictCode = doctor.DistrictCode,
                    EducationCountryCode = doctor.EducationCountryCode,
                    Email = doctor.Email,
                    EthnicityCode = doctor.EthnicityCode,
                    FullName = doctor.FullName,
                    Gender = doctor.Gender,
                    HisId = doctor.HisId,
                    NationCode = doctor.NationCode,
                    PhoneNumber = doctor.PhoneNumber,
                    PositionCode = doctor.PositionCode,
                    PriceDescription = doctor.PriceDescription,
                    PriceFrom = doctor.PriceFrom,
                    PriceTo = doctor.PriceTo,
                    ProvinceCode = doctor.ProvinceCode,
                    Summary = doctor.Summary,
                    UpdateDate = DateTime.Now,
                    UpdateUserId = doctor.UpdateUserId,
                    TitleCode = doctor.TitleCode,
                });

                if (string.IsNullOrEmpty(doctor.CertificationCode))
                {
                    _context.Update<Doctor>(d => d.DoctorId == doctor.DoctorId, x => new Doctor()
                    {
                        CertificationDate = null
                    });
                }

                if (checkCertificationDate == true)
                {
                    var date = DateTime.Parse(doctor.CertificationDate);
                    _context.Update<Doctor>(d => d.DoctorId == doctor.DoctorId, x => new Doctor()
                    {
                        CertificationDate = date
                    });
                }

                if (doctor.Avatar != "null")
                {
                    _context.Update<Doctor>(d => d.DoctorId == doctor.DoctorId, x => new Doctor()
                    {
                        Avatar = _fileUpload
                    });
                }

                if (allow == null)
                {
                    var specialist = _context.Query<DoctorSpecialists>().Where(s => s.IsDelete == false && s.IsActive == true && s.DoctorId == doctor.DoctorId);
                    foreach (var item in specialist.ToList())
                    {
                        _context.Update<DoctorSpecialists>(s => s.DoctorId == item.DoctorId, x => new DoctorSpecialists()
                        {
                            IsDelete = true
                        });
                    }

                    var healthfacilities = _context.Query<HealthFacilitiesDoctors>().Where(hf => hf.IsDelete == false && hf.IsActive == true && hf.DoctorId == doctor.DoctorId);
                    foreach (var item in healthfacilities.ToList())
                    {
                        _context.Update<HealthFacilitiesDoctors>(hf => hf.DoctorId == item.DoctorId, x => new HealthFacilitiesDoctors()
                        {
                            IsDelete = true
                        });
                    }

                    string[] specials;
                    if (!string.IsNullOrEmpty(doctor.Specials))
                    {
                        specials = doctor.Specials.Split(',');
                        foreach (var item in specials)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                _context.Insert(() => new DoctorSpecialists()
                                {
                                    DoctorId = doctor.DoctorId,
                                    SpecialistCode = item
                                });
                            }
                        }
                    }

                    string[] healhs;

                    if (!string.IsNullOrEmpty(doctor.Healths))
                    {
                        healhs = doctor.Healths.Split(',');
                        foreach (var item in healhs)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                _context.Insert(() => new HealthFacilitiesDoctors()
                                {
                                    DoctorId = doctor.DoctorId,
                                    HealthFacilitiesId = Convert.ToInt32(item)
                                });
                            }
                        }
                    }
                }

                _context.Session.CommitTransaction();
            }
            catch (Exception e)
            {
                if (_context.Session.IsInTransaction)
                {
                    _context.Session.RollbackTransaction();
                }
                return StatusCode(500, _excep.Throw("Có lỗi xảy ra", e.Message));
            }

            return Json(new ActionResultDto());
        }

        [HttpDelete]
        [Route("api/doctor")]
        public IActionResult Delete(int id)
        {
            var cc = _context.Query<Doctor>().Where(c => c.DoctorId == id).FirstOrDefault();

            if (cc == null)
                return StatusCode(404, _excep.Throw("Not Found"));

            List<string> clause = new List<string>();
            List<DbParam> param = new List<DbParam>();

            string doctorCheckBooking = @"SELECT Status FROM " + nameData + @".cats_doctors d INNER JOIN "+ nameData + @".booking_doctors_calendars bdc ON d.DoctorId=bdc.DoctorId WHERE 1=1 AND d.IsDelete=0 AND bdc.Status IN (0,1) ";

            clause.Add("AND d.DoctorId=@DoctorId");
            param.Add(new DbParam("@DoctorId", id));

            var str = $"{doctorCheckBooking} {string.Join(" ", clause)}";

            var reader = _context.Session.ExecuteReader(str, param);

            if (reader.Read() == true)
            {
                return StatusCode(406, _excep.Throw(406, "Xóa không thành công!", "Không thể xóa bác sĩ đang có lịch khám!"));
            }
            else
            {
                try
                {
                    _context.Session.CurrentConnection.Close();

                    _context.Session.BeginTransaction();

                    _context.Update<Doctor>(d => d.DoctorId == id, x => new Doctor()
                    {
                        IsDelete = true,
                        IsActive = false
                    });

                    var specialist = _context.Query<DoctorSpecialists>().Where(s => s.IsDelete == false && s.IsActive == true && s.DoctorId == id);
                    foreach (var item in specialist.ToList())
                    {
                        _context.Update<DoctorSpecialists>(s => s.DoctorId == item.DoctorId, x => new DoctorSpecialists()
                        {
                            IsDelete = true,
                            IsActive = false
                        });
                    }

                    var healthfacilities = _context.Query<HealthFacilitiesDoctors>().Where(hf => hf.IsDelete == false && hf.IsActive == true && hf.DoctorId == id);
                    foreach (var item in healthfacilities.ToList())
                    {
                        _context.Update<HealthFacilitiesDoctors>(hf => hf.DoctorId == item.DoctorId, x => new HealthFacilitiesDoctors()
                        {
                            IsDelete = true,
                            IsActive = false
                        });
                    }

                    var bookingDoctor = _context.Query<BookingDoctorsCalendars>().Where(bdc => bdc.Status != 0 && bdc.Status != 1 && bdc.IsDelete == false && bdc.IsActive == true && bdc.DoctorId == id);
                    foreach(var item in bookingDoctor.ToList())
                    {
                        _context.Update<BookingDoctorsCalendars>(bdc => bdc.DoctorId == item.DoctorId, x => new BookingDoctorsCalendars()
                        {
                            IsActive = false,
                            IsDelete = true
                        });
                    }

                    _context.Session.CommitTransaction();
                }
                catch (Exception e)
                {
                    if (_context.Session.IsInTransaction)
                    {
                        _context.Session.RollbackTransaction();
                    }
                    return StatusCode(500, _excep.Throw("Có lỗi xảy ra", e.Message));
                }
                return Json(new ActionResultDto());
            }
        }

        #endregion Old_Code

        private string GetUniqueFileName(string fileName)
        {
            fileName = convertToUnSign(Path.GetFileName(fileName));
            return Path.GetFileNameWithoutExtension(fileName)
                      + "_"
                      + Guid.NewGuid().ToString().Substring(0, 4)
                      + Path.GetExtension(fileName);
        }

        public string convertToUnSign(string s)
        {
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string first = regex.Replace(s.Normalize(NormalizationForm.FormD), String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
            return replaceSpace(first);
        }

        public string replaceSpace(string s)
        {
            s = s.Replace(" ", "_");
            return s;
        }
    }
}