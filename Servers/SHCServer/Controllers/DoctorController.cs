﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SHCServer.Models;
using SHCServer.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using Viettel;
using Viettel.MySql;

namespace SHCServer.Controllers
{
    public class DoctorController : BaseController
    {
        private readonly string _connectionString;

        public DoctorController(IOptions<Audience> settings, IConfiguration configuration)
        {
            _settings = settings;
            _context = new MySqlContext(new MySqlConnectionFactory(configuration.GetConnectionString("DefaultConnection")));
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _excep = new FriendlyException();
        }

        [HttpGet]
        [Route("api/doctor")]
        public IActionResult GetAll(int skipCount = 0, int maxResultCount = 10, string sorting = null, string filter = null)
        {
            var objs = _context.Query<Doctor>().Where(b => b.IsDelete == false)
                .LeftJoin<HealthFacilitiesDoctors>((b, s) => b.DoctorId == s.DoctorId)
                .LeftJoin<DoctorSpecialists>((b, s, d) => b.DoctorId == d.DoctorId)
                .Select((b, s, d) => new
                {
                    b.DoctorId,
                    b.FullName,
                    b.AllowSearch,
                    b.Address,
                    b.IsDelete,
                    b.BirthDate,
                    b.BirthMonth,
                    b.BirthYear,
                    s.HealthFacilitiesId,
                    b.Avatar,
                    b.AllowBooking,
                    b.AllowFilter,
                    d.SpecialistCode,
                    b.DistrictCode,
                    b.ProvinceCode,
                    b.PhoneNumber,
                    b.CreateDate
                });

            objs = objs.Where(b => b.IsDelete == false);
                
            if (filter != null)
            {
                foreach (var (key, value) in JsonConvert.DeserializeObject<Dictionary<string, string>>(filter))
                {
                    if (string.Equals(key, "doctorId") && !string.IsNullOrEmpty(value))
                    {
                        objs = objs.Where(b => b.DoctorId.ToString() == value.Trim());
                    }
                    if (string.Equals(key, "provinceCode") && !string.IsNullOrEmpty(value))
                    {
                        objs = objs.Where(b => b.ProvinceCode.ToString() == value.Trim() || b.ProvinceCode.ToString() == null || b.ProvinceCode.ToString() == "");
                    }
                    if (string.Equals(key, "districtCode") && !string.IsNullOrEmpty(value))
                    {
                        objs = objs.Where(b => b.DistrictCode.ToString() == value.Trim() || b.DistrictCode.ToString() == null || b.DistrictCode.ToString() == "");
                    }
                    if (string.Equals(key, "healthfacilities") && !string.IsNullOrEmpty(value))
                    {
                        objs = objs.Where(b => b.HealthFacilitiesId.ToString() == value.Trim() || b.HealthFacilitiesId.ToString() == null);
                    }
                    if (string.Equals(key, "specialistCode") && !string.IsNullOrEmpty(value))
                    {
                        //clause.Add("AND ds.SpecialistCode=@SpecialistCode");
                        //param.Add(DbParam.Create("@SpecialistCode", value.Trim()));
                        objs = objs.Where(b => b.SpecialistCode.ToString() == value.Trim());
                    }
                    if (string.Equals(key, "info") && !string.IsNullOrEmpty(value))
                    {
                        //clause.Add("AND d.FullName like '%' @FullNameOrPhone '%' OR d.PhoneNumber like '%' @FullNameOrPhone");
                        //param.Add(DbParam.Create("@fullNameOrPhone", value.Trim()));
                        objs = objs.Where(b => (b.FullName.ToString() == value.Trim()) || (b.PhoneNumber.ToString() == value.Trim()));
                    }
                }
            }

            //objs = objs.GroupBy(b => b.DoctorId).Select(b => new
            //{
            //    b.DoctorId,
            //    b.FullName,
            //    b.AllowSearch,
            //    b.Address,
            //    b.BirthDate,
            //    b.BirthMonth,
            //    b.BirthYear,
            //    b.HealthFacilitiesId,
            //    b.Avatar,
            //    b.AllowBooking,
            //    b.AllowFilter,
            //    b.SpecialistCode,
            //    b.DistrictCode,
            //    b.ProvinceCode,
            //    b.PhoneNumber,
            //    b.CreateDate
            //});

            if (sorting != null)
            {
                foreach (var (key, value) in JsonConvert.DeserializeObject<Dictionary<string, string>>(sorting))
                {
                    if (!Utils.PropertyExists<Doctor>(key))
                        continue;

                    objs = value == "asc" ? objs.OrderBy(u => key) : objs.OrderByDesc(u => key);
                }
            }
            else
            {
                objs = objs.OrderByDesc(b => b.CreateDate).ThenByDesc(b => b.FullName);
            }

            return Json(new ActionResultDto { Result = new { Items = objs.TakePage(skipCount == 0 ? 1 : skipCount + 1, maxResultCount).ToList(), TotalCount = objs.Count() } });

        }

        //[HttpGet]
        //[Route("api/doctor")]
        //public IActionResult GetAll(int skipCount = 0, int maxResultCount = 10, string sorting = null, string filter = null)
        //{
        //    List<string> clause = new List<string>();
        //    List<DbParam> param = new List<DbParam>();
        //    List<DoctorViewModel> doctorList = new List<DoctorViewModel>();

        //    int query = 0;

        //    if (filter != null)
        //    {
        //        foreach (var (key, value) in JsonConvert.DeserializeObject<Dictionary<string, string>>(filter))
        //        {
        //            if (string.IsNullOrEmpty(value))
        //                continue;
        //            if (string.Equals(key, "doctorId"))
        //            {
        //                clause.Add("AND d.DoctorId=@DoctorId");
        //                param.Add(DbParam.Create("@DoctorId", value.Trim()));
        //                query = 2;
        //            }
        //            if (string.Equals(key, "provinceCode"))
        //            {
        //                clause.Add("AND d.ProvinceCode=@ProvinceCode");
        //                param.Add(DbParam.Create("@ProvinceCode", value.Trim()));
        //            }
        //            if (string.Equals(key, "districtCode"))
        //            {
        //                clause.Add("AND d.DistrictCode=@DistrictCode");
        //                param.Add(DbParam.Create("@DistrictCode", value.Trim()));
        //            }
        //            if (string.Equals(key, "healthFacilitiesId"))
        //            {
        //                clause.Add("AND hd.HealthFacilitiesId=@HealthFacilitiesId");
        //                param.Add(DbParam.Create("@HealthFacilitiesId", value.Trim()));
        //                query = 1;
        //            }
        //            if (string.Equals(key, "specialistCode"))
        //            {
        //                clause.Add("AND ds.SpecialistCode=@SpecialistCode");
        //                param.Add(DbParam.Create("@SpecialistCode", value.Trim()));
        //            }
        //            if (string.Equals(key, "info"))
        //            {
        //                clause.Add("AND d.FullName like '%' @FullNameOrPhone '%' OR d.PhoneNumber like '%' @FullNameOrPhone");
        //                param.Add(DbParam.Create("@fullNameOrPhone", value.Trim()));
        //            }
        //        }
        //    }

        //    if (query == 0)
        //    {
        //        clause.Add(" AND ds.IsDelete=0 GROUP BY d.DoctorId ORDER BY d.FullName,d.CreateDate DESC LIMIT @skipCount, @resultCount");
        //    }

        //    if (query == 1)
        //    {
        //        clause.Add(" AND ds.IsDelete=0 GROUP BY d.DoctorId ORDER BY d.FullName,d.CreateDate DESC LIMIT @skipCount, @resultCount");
        //    }

        //    param.Add(DbParam.Create("@skipCount", skipCount * maxResultCount));
        //    param.Add(DbParam.Create("@resultCount", maxResultCount));

        //    var str = "";

        //    if (query == 0)
        //    {
        //        str = $"{DoctorJoin()} {string.Join(" ", clause)}";
        //    }

        //    if (query == 1)
        //    {
        //        str = $"{DoctorJoin(true)} {string.Join(" ", clause)}";
        //    }

        //    if (query == 2)
        //    {
        //        var checkHelthFacilites = _context.Query<HealthFacilitiesDoctors>().Where(hf => hf.DoctorId == (int)param[0].Value).FirstOrDefault();

        //        if (checkHelthFacilites != null)
        //            str = $"{DoctorJoin(true)} {string.Join(" ", clause)}";
        //        else
        //            str = $"{DoctorJoin()} {string.Join(" ", clause)}";
        //    }

        //    var reader = _context.Session.ExecuteReader(str, param);

        //    if (query == 2)
        //    {
        //        while (reader.Read())
        //        {
        //            var checkHelthFacilites = _context.Query<HealthFacilitiesDoctors>().Where(hf => hf.DoctorId == (int)param[0].Value).FirstOrDefault();

        //            if (checkHelthFacilites == null)
        //            {
        //                AddTolist(doctorList, reader);
        //            }
        //            else
        //            {
        //                AddTolistAll(doctorList, reader);
        //            }
        //        }
        //    }
        //    else if (query == 1)
        //    {
        //        while (reader.Read())
        //        {
        //            AddTolistAll(doctorList, reader);
        //        }
        //    }
        //    else
        //    {
        //        while (reader.Read())
        //        {
        //            AddTolist(doctorList, reader);
        //        }
        //    }

        //    return Json(new ActionResultDto()
        //    {
        //        Result = new
        //        {
        //            Items = doctorList,
        //            TotalCount = doctorList.Count
        //        }
        //    });
        //}

        [HttpPost]
        [Route("api/doctor")]
        #region Old_Code
        public IActionResult Create([FromBody] DoctorViewModel doctor)
        {
            try
            {
                _context.Session.BeginTransaction();

                _context.Insert(() => new Doctor()
                {
                    AcademicId = doctor.AcademicId,
                    Address = doctor.Address,
                    AllowBooking = doctor.AllowBooking,
                    AllowFilter = doctor.AllowFilter,
                    AllowSearch = doctor.AllowSearch,
                    Avatar = doctor.Avatar,
                    BirthDate = doctor.BirthDate,
                    BirthMonth = doctor.BirthMonth,
                    BirthYear = doctor.BirthYear,
                    CertificationCode = doctor.CertificationCode,
                    CertificationDate = doctor.CertificationDate,
                    CreateUserId = doctor.CreateUserId,
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

                var doctorFirst = _context.Query<Doctor>().OrderByDesc(d => d.CreateDate).FirstOrDefault();

                foreach (var item in doctor.DoctorSpecialists)
                {
                    _context.Insert(() => new DoctorSpecialists()
                    {
                        DoctorId = doctorFirst.DoctorId,
                        SpecialistCode = item
                    });
                }

                if (doctor.HealthFacilities != null)
                {
                    foreach (var h in doctor.HealthFacilities)
                    {
                        _context.Insert(() => new HealthFacilitiesDoctors()
                        {
                            HealthFacilitiesId = h.Value,
                            DoctorId = doctorFirst.DoctorId
                        });
                    }
                }

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
        public IActionResult Update([FromBody] DoctorViewModel doctor,int? allow)
        {
            var checkDoctor = _context.Query<Doctor>().Where(d => d.DoctorId == doctor.DoctorId).FirstOrDefault();

            if (checkDoctor == null)
            {
                return StatusCode(404, _excep.Throw("Not Found"));
            }
            try
            {
                _context.BeginTransaction();

                _context.Update<Doctor>(d => d.DoctorId == doctor.DoctorId, x => new Doctor()
                {
                    AcademicId = doctor.AcademicId,
                    Address = doctor.Address,
                    AllowBooking = doctor.AllowBooking,
                    AllowFilter = doctor.AllowFilter,
                    AllowSearch = doctor.AllowSearch,
                    Avatar = doctor.Avatar,
                    BirthDate = doctor.BirthDate,
                    BirthMonth = doctor.BirthMonth,
                    BirthYear = doctor.BirthYear,
                    CertificationCode = doctor.CertificationCode,
                    CertificationDate = doctor.CertificationDate,
                    CreateUserId = doctor.CreateUserId,
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

                if(allow==null)
                {
                    var getListHealthF = _context.Query<HealthFacilitiesDoctors>().Where(h => h.DoctorId == doctor.DoctorId);

                    if (doctor.HealthFacilities == null && getListHealthF.Count() > 0)
                    {
                        foreach (var item in getListHealthF.ToList())
                        {
                            _context.Update<HealthFacilitiesDoctors>(h => h.DoctorId == doctor.DoctorId && h.HealthFacilitiesId == doctor.HealthFacilitiesId, x => new HealthFacilitiesDoctors()
                            {
                                IsDelete = true
                            });
                        }
                    }

                    if (doctor.HealthFacilities != null && getListHealthF.Count() == 0)
                    {
                        foreach (var item in doctor.HealthFacilities)
                        {
                            _context.Insert(() => new HealthFacilitiesDoctors()
                            {
                                HealthFacilitiesId = (int)item,
                                DoctorId = doctor.DoctorId
                            });
                        }
                    }

                    if (doctor.HealthFacilities != null && getListHealthF.Count() > 0)
                    {
                        bool insert = false;
                        bool check = false;
                        bool delete = false;
                        List<int> index = new List<int>();
                        foreach (var item in getListHealthF.ToList())
                        {
                            for (int i = 0; i < doctor.HealthFacilities.Length; i++)
                            {
                                if (check == false)
                                {
                                    var diff = getListHealthF.Where(h => h.HealthFacilitiesId == doctor.HealthFacilities[i]).FirstOrDefault();

                                    if (diff == null)
                                    {
                                        insert = true;
                                        index.Add(i);
                                    }
                                }

                                if (item.HealthFacilitiesId != doctor.HealthFacilities[i])
                                {
                                    delete = true;
                                }
                            }
                            if (insert == true && check == false)
                            {
                                foreach (var item3 in index)
                                {
                                    _context.Insert(new HealthFacilitiesDoctors()
                                    {
                                        DoctorId = doctor.DoctorId,
                                        HealthFacilitiesId = (int)doctor.HealthFacilities[item3]
                                    });
                                }
                                insert = false;
                            }
                            if (delete == true)
                            {
                                _context.Update<HealthFacilitiesDoctors>(h => h.DoctorId == doctor.DoctorId && h.HealthFacilitiesId == item.HealthFacilitiesId, x => new HealthFacilitiesDoctors()
                                {
                                    IsDelete = true
                                });
                                delete = false;
                            }
                        }
                        check = true;
                    }

                    var getListSpecialist = _context.Query<DoctorSpecialists>().Where(h => h.DoctorId == doctor.DoctorId);

                    if (doctor.DoctorSpecialists == null && getListSpecialist.Count() > 0)
                    {
                        foreach (var item in getListSpecialist.ToList())
                        {
                            _context.Update<DoctorSpecialists>(h => h.DoctorId == doctor.DoctorId && h.SpecialistCode == doctor.SpecialistCode, x => new DoctorSpecialists()
                            {
                                IsDelete = true
                            });
                        }
                    }

                    if (doctor.DoctorSpecialists != null && getListSpecialist.Count() == 0)
                    {
                        foreach (var item in doctor.DoctorSpecialists)
                        {
                            _context.Insert(() => new DoctorSpecialists()
                            {
                                SpecialistCode = item,
                                DoctorId = doctor.DoctorId
                            });
                        }
                    }

                    if (doctor.DoctorSpecialists != null && getListSpecialist.Count() > 0)
                    {
                        bool insert = false;
                        bool check = false;
                        bool delete = false;
                        List<int> index = new List<int>();
                        foreach (var item in getListSpecialist.ToList())
                        {
                            for (int i = 0; i < doctor.DoctorSpecialists.Length; i++)
                            {
                                if (check == false)
                                {
                                    var diff = getListSpecialist.Where(h => h.DoctorId == doctor.DoctorId && h.SpecialistCode == doctor.DoctorSpecialists[i]).FirstOrDefault();

                                    if (diff == null)
                                    {
                                        insert = true;
                                        index.Add(i);
                                    }
                                }

                                if (item.SpecialistCode != doctor.DoctorSpecialists[i])
                                {
                                    delete = true;
                                }
                            }
                            if (insert == true && check == false)
                            {
                                foreach (var item3 in index)
                                {
                                    _context.Insert(new DoctorSpecialists()
                                    {
                                        DoctorId = doctor.DoctorId,
                                        SpecialistCode = doctor.DoctorSpecialists[item3]
                                    });
                                }
                                insert = false;
                            }
                            if (delete == true)
                            {
                                _context.Update<DoctorSpecialists>(h => h.DoctorId == doctor.DoctorId && h.SpecialistCode == item.SpecialistCode, x => new DoctorSpecialists()
                                {
                                    IsDelete = true
                                });
                                delete = false;
                            }
                        }
                        check = true;
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

            string doctorCheckBooking = @"SELECT Status FROM smarthealthcare.cats_doctors d INNER JOIN smarthealthcare.booking_doctors_calendars bdc ON d.DoctorId=bdc.DoctorId WHERE 1=1 AND d.IsDelete=0 AND bdc.Status IN (0,1) ";

            clause.Add("AND d.DoctorId=@DoctorId");
            param.Add(new DbParam("@DoctorId", id));

            var str = $"{doctorCheckBooking} {string.Join(" ", clause)}";

            var reader = _context.Session.ExecuteReader(str, param);

            if (reader.Read() == true)
            {
                return StatusCode(400, _excep.Throw("Xóa không thành công!", "Không thể xóa bác sĩ đang có lịch khám!"));
            }
            else
            {
                try
                {
                    _context.Session.CurrentConnection.Close();

                    _context.Session.BeginTransaction();

                    _context.Update<Doctor>(d => d.DoctorId == id, x => new Doctor()
                    {
                        IsDelete = true
                    });

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

        private void AddTolist(List<DoctorViewModel> doctorList, IDataReader reader)
        {
            doctorList.Add(new DoctorViewModel()
            {
                AcademicId = reader["AcademicId"] != DBNull.Value ? Convert.ToInt32(reader["AcademicId"]) : 0,
                Address = reader["Address"].ToString(),
                AllowBooking = Convert.ToInt16(reader["AllowBooking"]) == 0 ? false : true,
                AllowFilter = Convert.ToInt16(reader["AllowFilter"]) == 0 ? false : true,
                AllowSearch = Convert.ToInt16(reader["AllowSearch"]) == 0 ? false : true,
                Avatar = reader["Avatar"].ToString(),
                BirthDate = reader["BirthDate"] != DBNull.Value ? Convert.ToInt16(reader["BirthDate"]) : 0,
                BirthMonth = reader["BirthDate"] != DBNull.Value ? Convert.ToInt16(reader["BirthMonth"]) : 0,
                BirthYear = Convert.ToInt16(reader["BirthYear"]),
                CertificationCode = reader["CertificationCode"].ToString(),
                CertificationDate = reader["CertificationDate"] != DBNull.Value ? Convert.ToDateTime(reader["CertificationDate"]) : Convert.ToDateTime("0001-01-01T01:01:01"),
                DegreeId = reader["DegreeId"] != DBNull.Value ? Convert.ToInt16(reader["DegreeId"]) : 0,
                Description = reader["Description"].ToString(),
                DistrictCode = reader["DistrictCode"].ToString(),
                DoctorId=Convert.ToInt16(reader["DoctorId"]),
                ProvinceCode = reader["ProvinceCode"].ToString(),
                EducationCountryCode = reader["EducationCountryCode"].ToString(),
                Email = reader["Email"].ToString(),
                EthnicityCode = reader["EthnicityCode"].ToString(),
                FullName = reader["FullName"].ToString(),
                Gender = Convert.ToInt16(reader["Gender"]),
                HisId = reader["HisId"].ToString(),
                IsActive = Convert.ToInt16(reader["IsActive"]) == 0 ? false : true,
                IsSync = Convert.ToInt16(reader["IsSync"]) == 0 ? false : true,
                NationCode = reader["NationCode"].ToString(),
                PhoneNumber = reader["PhoneNumber"].ToString(),
                PositionCode = reader["PositionCode"].ToString(),
                PriceDescription = reader["PriceDescription"].ToString(),
                PriceFrom = reader["PriceFrom"] != DBNull.Value ? Convert.ToInt32(reader["PriceFrom"]) : 0,
                PriceTo = reader["PriceTo"] != DBNull.Value ? Convert.ToInt32(reader["PriceTo"]) : 0,
                Summary = reader["Summary"].ToString(),
                TitleCode = reader["TitleCode"].ToString(),
                SpecialistName = reader["SpecialistName"].ToString(),
            });
        }

        private void AddTolistAll(List<DoctorViewModel> doctorList, IDataReader reader)
        {
            doctorList.Add(new DoctorViewModel()
            {
                AcademicId = reader["AcademicId"] != DBNull.Value ? Convert.ToInt32(reader["AcademicId"]) : 0,
                Address = reader["Address"].ToString(),
                AllowBooking = Convert.ToInt16(reader["AllowBooking"]) == 0 ? false : true,
                AllowFilter = Convert.ToInt16(reader["AllowFilter"]) == 0 ? false : true,
                AllowSearch = Convert.ToInt16(reader["AllowSearch"]) == 0 ? false : true,
                Avatar = reader["Avatar"].ToString(),
                BirthDate = reader["BirthDate"] != DBNull.Value ? Convert.ToInt16(reader["BirthDate"]) : 0,
                BirthMonth = reader["BirthDate"] != DBNull.Value ? Convert.ToInt16(reader["BirthMonth"]) : 0,
                BirthYear = Convert.ToInt16(reader["BirthYear"]),
                CertificationCode = reader["CertificationCode"].ToString(),
                CertificationDate = reader["CertificationDate"] != DBNull.Value ? Convert.ToDateTime(reader["CertificationDate"]) : Convert.ToDateTime("0001-01-01T01:01:01"),
                DegreeId = reader["DegreeId"] != DBNull.Value ? Convert.ToInt16(reader["DegreeId"]) : 0,
                Description = reader["Description"].ToString(),
                DistrictCode = reader["DistrictCode"].ToString(),
                DoctorId = Convert.ToInt16(reader["DoctorId"]),
                ProvinceCode = reader["ProvinceCode"].ToString(),
                EducationCountryCode = reader["EducationCountryCode"].ToString(),
                Email = reader["Email"].ToString(),
                EthnicityCode = reader["EthnicityCode"].ToString(),
                FullName = reader["FullName"].ToString(),
                Gender = Convert.ToInt16(reader["Gender"]),
                HisId = reader["HisId"].ToString(),
                IsActive = Convert.ToInt16(reader["IsActive"]) == 0 ? false : true,
                IsSync = Convert.ToInt16(reader["IsSync"]) == 0 ? false : true,
                NationCode = reader["NationCode"].ToString(),
                PhoneNumber = reader["PhoneNumber"].ToString(),
                PositionCode = reader["PositionCode"].ToString(),
                PriceDescription = reader["PriceDescription"].ToString(),
                PriceFrom = reader["PriceFrom"] != DBNull.Value ? Convert.ToInt32(reader["PriceFrom"]) : 0,
                PriceTo = reader["PriceTo"] != DBNull.Value ? Convert.ToInt32(reader["PriceTo"]) : 0,
                Summary = reader["Summary"].ToString(),
                TitleCode = reader["TitleCode"].ToString(),
                SpecialistName = reader["SpecialistName"].ToString(),
                HealthFacilitiesName = reader["HealthFacilitiesName"].ToString()
            });
        }

        private string DoctorJoin(bool joinHealthFacilities = false)
        {
            string query = @"SELECT
                                            d.DoctorId,
                                            AcademicId,
                                            d.Address,
                                            d.AllowBooking,
                                            d.AllowFilter,
                                            d.AllowSearch,
                                            Avatar,
                                            BirthDate,
                                            BirthMonth,
                                            BirthYear,
                                            CertificationCode,
                                            CertificationDate,
                                            DegreeId,
                                            d.Description,
                                            d.DistrictCode,
                                            d.ProvinceCode,
                                            EducationCountryCode,
                                            d.Email,
                                            EthnicityCode,
                                            FullName,
                                            Gender,
                                            HisId,
                                            d.IsActive,
                                            d.IsSync,
                                            NationCode,
                                            d.PhoneNumber,
                                            PositionCode,
                                            PriceDescription,
                                            PriceFrom,
                                            PriceTo,
                                            Summary,
                                            TitleCode,
                                            cc.Name as SpecialistName";
            string queryJoinSp = " FROM smarthealthcare.cats_doctors d INNER JOIN smarthealthcare.cats_doctors_specialists ds ON d.DoctorId = ds.DoctorId INNER JOIN smarthealthcare.cats_common cc ON ds.SpecialistCode=cc.Code WHERE 1 = 1 AND d.IsDelete = 0 AND ds.IsDelete = 0 ";
            string queryJoinSpH = @",hf.Name as HealthFacilitiesName 
                                    FROM smarthealthcare.cats_doctors d 
                                    INNER JOIN smarthealthcare.cats_doctors_specialists ds ON d.DoctorId = ds.DoctorId 
                                    INNER JOIN smarthealthcare.cats_healthfacilities_doctors hd ON d.DoctorId = hd.DoctorId 
                                    INNER JOIN smarthealthcare.cats_common cc ON ds.SpecialistCode=cc.Code 
                                    INNER JOIN smarthealthcare.cats_healthfacilities hf ON hd.HealthFacilitiesId=hf.HealthFacilitiesId WHERE 1 = 1 AND d.IsDelete = 0 AND ds.IsDelete = 0 ";
            if (joinHealthFacilities == false)
                return query + queryJoinSp;
            else
                return query + queryJoinSpH;
        }
        #endregion
    }
}