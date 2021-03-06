﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SHCServer.Models;
using SHCServer.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Viettel;
using Viettel.MySql;
using Group = SHCServer.Models.Group;

namespace SHCServer.Controllers
{
    public class UsersController : BaseController
    {
        private readonly string _connectionString;
        //private readonly string _connectionStringShc;
        private string _newPassword;
        private string nameData;
        public UsersController(IOptions<Audience> settings, IConfiguration configuration)
        {
            _settings = settings;
            _contextmdmdb = new MySqlContext(new MySqlConnectionFactory(configuration.GetConnectionString("MdmConnection")));
            //_connectionString = configuration.GetConnectionString("MdmConnection");
            _excep = new FriendlyException();
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _context = new MySqlContext(new MySqlConnectionFactory(_connectionString));
            //_connectionStringShc = configuration.GetConnectionString("DefaultConnection");

            if (_connectionString.IndexOf("smarthealthcare_58") > 0) nameData = "smarthealthcare_58";
            else if (_connectionString.IndexOf("smarthealthcare") > 0) nameData = "smarthealthcare";
        }

        [HttpGet]
        [Route("api/users")]
        public IActionResult Get(int skipCount = 0, int maxResultCount = 10, string sorting = null, string filter = null)
        {
            string query = @"SELECT 
                                    u.*,
                                    ui.Status as SHCStatus,
                                    g.GroupName
                                FROM " + nameData + @".sys_users ui
                                INNER JOIN mdm.sys_users u
                                ON ui.UserId = u.UserId
                                LEFT JOIN mdm.sys_users_groups ug ON u.UserId = ug.UserId AND ug.IsDelete = 0 
                                LEFT JOIN mdm.sys_groups g ON g.GroupId = ug.GroupId AND g.IsDelete = 0
                                WHERE u.IsDelete = 0
                                AND u.AccountType IN (1, 2, 3)
                                AND ui.IsDelete = 0 ";
            List<string> clause = new List<string>();
            List<DbParam> param = new List<DbParam>();
            List<UserViewModel> lst = new List<UserViewModel>();

            if (filter != null)
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(filter);

                if (data.ContainsKey("provinceCode"))
                {
                    if (!string.IsNullOrEmpty(data["provinceCode"]))
                    {
                        clause.Add("AND u.ProvinceCode = @provinceCode");
                        param.Add(DbParam.Create("@provinceCode", data["provinceCode"]));
                    } 
                }

                if (data.ContainsKey("districtCode"))
                {
                    if (!string.IsNullOrEmpty(data["districtCode"]))
                    {
                        clause.Add("AND u.DistrictCode = @districtCode");
                        param.Add(DbParam.Create("@districtCode", data["districtCode"]));
                    }
                }

                if (data.ContainsKey("wardCode"))
                {
                    if (!string.IsNullOrEmpty(data["wardCode"]))
                    {
                        clause.Add("AND u.WardCode = @wardCode");
                        param.Add(DbParam.Create("@wardCode", data["wardCode"]));
                    }
                }

                if (data.ContainsKey("userName"))
                {
                    if (!string.IsNullOrEmpty(data["userName"]))
                    {
                        clause.Add($"AND u.UserName LIKE '%{data["userName"].Replace(@"%", "\\%").Replace(@"_", "\\_").Trim()}%'");
                    }
                }

                if (data.ContainsKey("accountType"))
                {
                    if (!string.IsNullOrEmpty(data["accountType"]))
                    {
                        clause.Add("AND u.AccountType = @accountType");
                        param.Add(DbParam.Create("@accountType", data["accountType"]));
                    }
                   
                }

                if (data.ContainsKey("userPhoneEmail"))
                {
                    if (!string.IsNullOrEmpty(data["userPhoneEmail"]))
                    {
                        clause.Add($"AND (u.PhoneNumber = @userPhoneEmail OR u.Email = @userPhoneEmail)");
                        param.Add(DbParam.Create("@userPhoneEmail", data["userPhoneEmail"].Trim()));
                    }
                       
                }

                if (data.ContainsKey("group"))
                {
                    if (!string.IsNullOrEmpty(data["group"]))
                    {
                        var group = _contextmdmdb.Query<UserGroup>().Where(o => o.GroupId == int.Parse(data["group"].ToString()) && o.IsDelete == false).ToList();

                        if (group.Count > 0)
                        {
                            int count = group.Count;
                            clause.Add($"AND ( u.UserId = {group[0].UserId}");
                            for (int i = 1; i < count - 1; i++)
                            {
                                clause.Add($"OR u.UserId = {group[i].UserId}");
                            }
                            clause.Add($"OR u.UserId = {group[count - 1].UserId})");
                        }
                        else
                        {
                            return Json(new ActionResultDto());
                        }
                    }
                }
            }

            clause.Add("GROUP BY u.UserId");

            if (sorting != null)
            {
                //clause.Add(" ORDER BY u.CreateDate DESC ");

                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(sorting);

                if (data.ContainsKey("userId"))
                {
                    if (data["userId"] == "asc") clause.Add("ORDER BY u.UserId ASC");
                    else clause.Add("ORDER BY u.UserId DESC");
                }

                if (data.ContainsKey("fullName"))
                {
                    if (data["fullName"] == "asc") clause.Add("ORDER BY u.FullName ASC");
                    else clause.Add("ORDER BY u.FullName DESC");
                }

                if (data.ContainsKey("userName"))
                {
                    if (data["userName"] == "asc") clause.Add("ORDER BY u.UserName ASC");
                    else clause.Add("ORDER BY u.UserName DESC");
                }

                if (data.ContainsKey("accountType"))
                {
                    if (data["accountType"] == "asc") clause.Add("ORDER BY u.AccountType ASC");
                    else clause.Add("ORDER BY u.AccountType DESC");
                }
                if (data.ContainsKey("createDate"))
                {
                    if (data["createDate"] == "asc") clause.Add("ORDER BY u.CreateDate ASC");
                    else clause.Add("ORDER BY u.CreateDate DESC");
                }
            }
            
            var readerAll = _contextmdmdb.Session.ExecuteReader($"{query} {string.Join(" ", clause)}", param);
            var total = 0;

            while (readerAll.Read())
            {
                total++;
            }

            readerAll.Close();

            clause.Add("LIMIT @skipCount, @resultCount");
            param.Add(DbParam.Create("@skipCount", skipCount * maxResultCount));
            param.Add(DbParam.Create("@resultCount", maxResultCount));

            string strQuery = $"{query} {string.Join(" ", clause)}";
            var reader = _contextmdmdb.Session.ExecuteReader(strQuery, param);

            
            while (reader.Read())
            {
                lst.Add(new UserViewModel()
                {
                    UserId = Convert.ToInt32(reader["UserId"]),
                    UserName = reader["UserName"].ToString(),
                    Status = Convert.ToInt32(reader["Status"]),
                    StatusSHC = Convert.ToInt32(reader["SHCStatus"]),
                    FullName = reader["FullName"].ToString(),
                    PhoneNumber = reader["PhoneNumber"].ToString(),
                    Email = reader["Email"].ToString(),
                    Address = reader["Address"].ToString(),
                    AccountType = Convert.ToInt32(reader["AccountType"]),
                    ProvinceCode = reader["ProvinceCode"].ToString(),
                    WardCode = reader["WardCode"].ToString(),
                    DistrictCode = reader["DistrictCode"].ToString(),
                    GroupName = reader["GroupName"].ToString(),
                    Insurrance = reader["Insurrance"].ToString(),
                    Identification = reader["Identification"].ToString(),
                    LisenceCode = reader["LisenceCode"].ToString(),
                    CertificationCode = reader["CertificationCode"].ToString(),
                    BirthDay = reader["BirthDay"] != DBNull.Value ? DateTime.Parse(reader["BirthDay"].ToString()) : DateTime.Now,
                    Sex = Convert.ToInt32(reader["Sex"]),
                });
            }
            reader.Close();

            foreach (var item in lst)
            {
                if (!string.IsNullOrEmpty(item.ProvinceCode)) item.Locality = _contextmdmdb.Query<Province>().FirstOrDefault(o => o.ProvinceCode == item.ProvinceCode)!=null? _contextmdmdb.Query<Province>().FirstOrDefault(o => o.ProvinceCode == item.ProvinceCode).Name:"";
                if (!string.IsNullOrEmpty(item.DistrictCode)) item.Locality = _contextmdmdb.Query<District>().FirstOrDefault(o => o.DistrictCode == item.DistrictCode) != null ? $"{item.Locality} - {_contextmdmdb.Query<District>().FirstOrDefault(o => o.DistrictCode == item.DistrictCode).Name}" : "";
                if (!string.IsNullOrEmpty(item.WardCode)) item.Locality = _contextmdmdb.Query<Ward>().FirstOrDefault(o => o.WardCode == item.WardCode)!=null? $"{item.Locality} - {_contextmdmdb.Query<Ward>().FirstOrDefault(o => o.WardCode == item.WardCode) ? .Name}":"";

                item.Locality = (!string.IsNullOrEmpty(item.Locality) ? item.Locality : "");

                if (1 == item.AccountType) item.AccountTypeName = "Thành viên";
                else if (2 == item.AccountType) item.AccountTypeName = "Bác sĩ / Chuyên gia / Điều dưỡng";
                else if (3 == item.AccountType) item.AccountTypeName = "Cơ sở y tế / Doanh nghiệp";

                var lst1 = _contextmdmdb.JoinQuery<UserGroup, Group>((u, g) => new object[] { JoinType.InnerJoin, u.GroupId == g.GroupId }).Where((u, g) => u.IsDelete == false && u.UserId == item.UserId).Select((u, g) => g.GroupName).ToList();
                item.GroupName = String.Join(", ", lst1.ToArray());
            }

            return Json(new ActionResultDto { Result = new { Items = lst, TotalCount = total } });
        }

        [HttpPost]
        [Route("api/users-register")]
        public IActionResult Register([FromForm]UserInputViewModel obj)
        {
            obj.UserName = obj.UserName.Trim().ToLower();
            obj.Email = obj.Email.Trim().ToLower();
            if (!string.IsNullOrEmpty(obj.CertificationCode)) obj.CertificationCode = obj.CertificationCode.Trim().ToUpper();
            if (!string.IsNullOrEmpty(obj.Insurrance)) obj.Insurrance = obj.Insurrance.Trim().ToUpper();
            if (!string.IsNullOrEmpty(obj.LisenceCode)) obj.LisenceCode = obj.LisenceCode.Trim().ToUpper();


            var getUser = _contextmdmdb.Query<UserMDM>().Where(o => o.IsDelete == false);
            
            //tài khoản
            if (getUser.Where(u => u.UserName == obj.UserName.Trim() && !string.IsNullOrEmpty(u.UserName)).Any())
            {
                return StatusCode(406, _excep.Throw(406,"Thông báo", "Tài khoản đã tồn tại!"));
            }

            if (getUser.Where(u => u.PhoneNumber == obj.PhoneNumber.Trim() && !string.IsNullOrEmpty(u.PhoneNumber)).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số điện thoại đã tồn tại!"));
            }

            //email
            if (getUser.Where(u => u.Email == obj.Email.Trim() && !string.IsNullOrEmpty(u.Email)).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Email đã tồn tại!"));
            }

            if (getUser.Where(u => u.Identification == obj.Identification && !string.IsNullOrEmpty(u.Identification)).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số CMND đã tồn tại!"));
            }

            if (getUser.Where(u => u.CertificationCode == obj.CertificationCode && !string.IsNullOrEmpty(u.CertificationCode)).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số GPHN đã tồn tại!"));
            }

            if (getUser.Where(u => u.LisenceCode == obj.LisenceCode && !string.IsNullOrEmpty(u.LisenceCode)).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số GPKD đã tồn tại!"));
            }

            if (getUser.Where(u => u.Insurrance == obj.Insurrance && !string.IsNullOrEmpty(u.Insurrance)).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số thẻ BHYT  đã tồn tại!"));
            }

            try
            {
                // Insert sys_users in mdm
                _contextmdmdb.Session.BeginTransaction();
                _contextmdmdb.Insert(() => new UserMDM
                {
                    UserName = obj.UserName,
                    Password = Utils.HashPassword(obj.Password),
                    AccountType = obj.AccountType,

                    FullName = obj.FullName,
                    Sex = obj.Sex,
                    BirthDay = obj.BirthDay,

                    Email = obj.Email,
                    PhoneNumber = obj.PhoneNumber,
                    Address = obj.Address,

                    ProvinceCode = obj.ProvinceCode,
                    DistrictCode = obj.DistrictCode,
                    WardCode = obj.WardCode,

                    CreateUserId = obj.CreateUserId,
                    CreateDate = DateTime.Now,

                    CertificationCode = obj.CertificationCode,
                    Insurrance = obj.Insurrance,
                    Identification = obj.Identification,
                    LisenceCode = obj.LisenceCode
                });
                _contextmdmdb.Session.CommitTransaction();

                UserMDM user = _contextmdmdb.Query<UserMDM>().Where(u => u.UserName == obj.UserName && u.IsDelete == false).FirstOrDefault();
                int userId = user != null ? user.UserId : 0;

                // Insert sys_users_groups in mdm
                if (!string.IsNullOrEmpty(obj.GroupId))
                {
                    var groups = obj.GroupId.Split(',');
                    if (groups.Length > 0)
                    {
                        foreach (var item in groups)
                        {
                            string[] ids = item.Split('-');
                            if (ids.Length == 2)
                            {
                                _contextmdmdb.Session.BeginTransaction();
                                string subQuery = "INSERT INTO sys_users_groups (UserId, GroupId, ApplicationId) VALUES ";
                                var subParamQuery = new List<string>();
                                var subParam = new List<DbParam>();

                                subParamQuery.Add($"(@UserId, @GroupId, @ApplicationId)");
                                subParam.Add(DbParam.Create("@UserId", userId));
                                subParam.Add(DbParam.Create("@GroupId", int.Parse(ids[0].Trim())));
                                subParam.Add(DbParam.Create("@ApplicationId", int.Parse(ids[1].Trim())));
                                _contextmdmdb.Session.ExecuteNonQuery($"{subQuery} {string.Join(",", subParamQuery)}", subParam);
                                _contextmdmdb.Session.CommitTransaction();
                            }
                        }
                    }
                }

                // Insert sys_users_healthfacilities in smarthealthcare
                if (!string.IsNullOrEmpty(obj.healthId))
                {
                    var healthIds = obj.healthId.Split(',');
                    if (healthIds.Length > 0)
                    {
                        foreach (var item in healthIds)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                _context.Session.BeginTransaction();
                                _context.Insert(() => new UserHealthFacilities
                                {
                                    UserId = userId,
                                    HealthFacilitiesId = int.Parse(item.Trim()),
                                    CreateDate = DateTime.Now,
                                    CreateUserId = obj.CreateUserId
                                });
                                _context.Session.CommitTransaction();
                            }
                        }
                    }
                }

                // Insert sys_users in smarthealthcare
                _context.Session.BeginTransaction();
                _context.Insert(() => new User
                {
                    Status = 2,
                    UserId = userId,
                    ExpriredDate = DateTime.Now.AddMonths(2)
                });
                _context.Session.CommitTransaction();

                // Insert sys_users_attachs in smarthealthcare
                _context.Session.BeginTransaction();
                var files = Request.Form.Files;
                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        var uniqueFileName = GetUniqueFileName(ConvertToUnSign(file.FileName));
                        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        var filePath = Path.Combine(uploads, uniqueFileName);
                        if (file.Name == "cmnd")
                        {
                            _context.Insert(() => new UsersAttach
                            {
                                UserId = user.UserId,
                                Path = "/uploads/" + uniqueFileName,
                                Type = "1",
                                CreateUserId = obj.CreateUserId,
                                CreateDate = DateTime.Now
                            });
                        }
                        else
                        {
                            if (obj.AccountType == 2)
                            {
                                _context.Insert(() => new UsersAttach
                                {
                                    UserId = user.UserId,
                                    Path = "/uploads/" + uniqueFileName,
                                    Type = "3",
                                    CreateUserId = obj.CreateUserId,
                                    CreateDate = DateTime.Now
                                });
                            }
                            else if (obj.AccountType == 3)
                            {
                                _context.Insert(() => new UsersAttach
                                {
                                    UserId = user.UserId,
                                    Path = "/uploads/" + uniqueFileName,
                                    Type = "4",
                                    CreateUserId = obj.CreateUserId,
                                    CreateDate = DateTime.Now
                                });
                            }
                            else
                            {
                                _context.Insert(() => new UsersAttach
                                {
                                    UserId = user.UserId,
                                    Path = "/uploads/" + uniqueFileName,
                                    Type = "2",
                                    CreateUserId = obj.CreateUserId,
                                    CreateDate = DateTime.Now
                                });
                            }
                        }
                        file.CopyTo(new FileStream(filePath, FileMode.Create));
                    }
                }
                _context.Session.CommitTransaction();

                return Json(new ActionResultDto());
            }
            catch (Exception e)
            {
                if (_context.Session.IsInTransaction) _context.Session.RollbackTransaction();
                if (_contextmdmdb.Session.IsInTransaction) _contextmdmdb.Session.RollbackTransaction();
                return StatusCode(500, _excep.Throw("Thông báo", e.Message));
            }
        }

        [HttpPut]
        [Route("api/users-update")]
        public ActionResult UpdateUser([FromForm]UserInputViewModel obj)
        {
            var user = _contextmdmdb.Query<UserMDM>();

            if (user.FirstOrDefault(u => u.UserId == obj.UserId) == null)
            {
                return StatusCode(401, _excep.Throw(406, "Thông báo", "Tài khoản không tồn tại!"));
            }

            if (user.Where(u => u.PhoneNumber == obj.PhoneNumber && !string.IsNullOrEmpty(u.PhoneNumber) && u.UserId != obj.UserId && u.IsDelete == false).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số điện thoại đã tồn tại!"));
            }

            if (user.Where(u => u.Email == obj.Email && !string.IsNullOrEmpty(u.Email) && u.UserId != obj.UserId && u.IsDelete == false).Count() > 0)
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Email đã tồn tại!"));
            }

            if (user.Where(u => u.Identification == obj.Identification && !string.IsNullOrEmpty(u.Identification) && u.UserId != obj.UserId && u.IsDelete == false).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số CMND đã tồn tại!"));
            }

            if (user.Where(u => u.CertificationCode == obj.CertificationCode && !string.IsNullOrEmpty(u.CertificationCode) && u.UserId != obj.UserId && u.IsDelete == false).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số GPHN đã tồn tại!"));
            }

            if (user.Where(u => u.LisenceCode == obj.LisenceCode && !string.IsNullOrEmpty(u.LisenceCode) && u.UserId != obj.UserId && u.IsDelete == false).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số GPKD đã tồn tại!"));
            }

            if (user.Where(u => u.Insurrance == obj.Insurrance && !string.IsNullOrEmpty(u.Insurrance) && u.UserId != obj.UserId && u.IsDelete == false).Any())
            {
                return StatusCode(406, _excep.Throw(406, "Thông báo", "Số thẻ BHYT  đã tồn tại!"));
            }

            try
            {
                // Update sys_users in mdm
                _contextmdmdb.Session.BeginTransaction();

                _contextmdmdb.Update<UserMDM>(g => g.UserId == obj.UserId, a => new UserMDM
                {
                    FullName = obj.FullName,
                    PhoneNumber = obj.PhoneNumber,
                    Email = obj.Email,
                    Sex = obj.Sex,
                    BirthDay = obj.BirthDay,
                    AccountType = obj.AccountType,

                    ProvinceCode = obj.ProvinceCode,
                    DistrictCode = obj.DistrictCode,
                    WardCode = obj.WardCode,
                    Address = obj.Address,

                    UpdateDate = DateTime.Now,
                    UpdateUserId = obj.UpdateUserId,

                    CertificationCode = obj.CertificationCode,
                    Insurrance = obj.Insurrance,
                    Identification = obj.Identification,
                    LisenceCode = obj.LisenceCode
                });

                _contextmdmdb.Session.CommitTransaction();

                UserMDM userMdm = _contextmdmdb.Query<UserMDM>().Where(u => u.UserName == obj.UserName).FirstOrDefault();
                int userId = userMdm != null ? userMdm.UserId : 0;

                // Update sys_users_groups in mdm
                if (!string.IsNullOrEmpty(obj.GroupId))
                {
                    var groups = obj.GroupId.Split(',');
                    if (groups.Length > 0)
                    {
                        var usersGroup = _contextmdmdb.Query<UserGroup>().Where(p => p.UserId == obj.UserId).Select(q => q.GroupId).ToList();

                        foreach (var item in groups)
                        {
                            string[] ids = item.Split('-');
                            if (ids.Length == 2)
                            {
                                _contextmdmdb.Session.BeginTransaction();

                                int idGroup = int.Parse(ids[0].Trim());
                                int idApp = int.Parse(ids[1].Trim());


                                if (usersGroup.Contains(idGroup))
                                {
                                    _contextmdmdb.Update<UserGroup>(g => g.UserId == obj.UserId, a => new UserGroup
                                    {
                                        IsDelete = false,
                                        UpdateDate = DateTime.Now,
                                        UpdateUserId = obj.UpdateUserId
                                    });

                                    usersGroup.Remove(idGroup);
                                }
                                else
                                {
                                    _contextmdmdb.Insert(() => new UserGroup
                                    {
                                        UserId = int.Parse(obj.UserId.ToString()),
                                        GroupId = idGroup,
                                        ApplicationId = idApp,
                                        CreateDate = DateTime.Now,
                                        CreateUserId = obj.UpdateUserId
                                    });
                                }
                                _contextmdmdb.Session.CommitTransaction();
                            }
                        }

                        foreach (var item in usersGroup)
                        {
                            _contextmdmdb.Session.BeginTransaction();
                            _contextmdmdb.Update<UserGroup>(g => g.UserId == obj.UserId && g.GroupId == item, a => new UserGroup
                            {
                                IsDelete = true,
                                UpdateDate = DateTime.Now,
                                UpdateUserId = obj.UpdateUserId
                            });
                            _contextmdmdb.Session.CommitTransaction();
                        }
                    }
                }
                else
                {
                    _contextmdmdb.Session.BeginTransaction();
                    _contextmdmdb.Update<UserGroup>(g => g.UserId == obj.UserId, a => new UserGroup
                    {
                        IsDelete = true,
                        UpdateDate = DateTime.Now,
                        UpdateUserId = obj.UpdateUserId
                    });
                    _contextmdmdb.Session.CommitTransaction();
                }

                // Update sys_users_healthfacilities in smarthealthcare
                if (!string.IsNullOrEmpty(obj.healthId))
                {
                    var healthIds = obj.healthId.Split(',');
                    if (healthIds.Length > 0)
                    {
                        var usersHeal = _context.Query<UserHealthFacilities>().Where(p => p.UserId == obj.UserId).Select(q => q.HealthFacilitiesId).ToList();

                        foreach (var item in healthIds)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                int idHeal = int.Parse(item.Trim());
                                _context.Session.BeginTransaction();
                                if (usersHeal.Contains(idHeal))
                                {
                                    _context.Update<UserHealthFacilities>(g => g.UserId == obj.UserId, a => new UserHealthFacilities
                                    {
                                        IsDelete = false,
                                        UpdateDate = DateTime.Now,
                                        UpdateUserId = obj.UpdateUserId
                                    });

                                    usersHeal.Remove(idHeal);
                                }
                                else
                                {
                                    _context.Insert(() => new UserHealthFacilities
                                    {
                                        UserId = userId,
                                        HealthFacilitiesId = idHeal,
                                        CreateDate = DateTime.Now,
                                        CreateUserId = obj.UpdateUserId
                                    });
                                }
                                _context.Session.CommitTransaction();
                            }
                        }

                        foreach (var item in usersHeal)
                        {
                            _context.Session.BeginTransaction();
                            _context.Update<UserHealthFacilities>(g => g.UserId == obj.UserId && g.HealthFacilitiesId == item, a => new UserHealthFacilities
                            {
                                IsDelete = true,
                                UpdateDate = DateTime.Now,
                                UpdateUserId = obj.UpdateUserId
                            });
                            _context.Session.CommitTransaction();
                        }
                    }
                }
                else
                {
                    _context.Session.BeginTransaction();
                    _context.Update<UserHealthFacilities>(g => g.UserId == obj.UserId, a => new UserHealthFacilities
                    {
                        IsDelete = true,
                        UpdateDate = DateTime.Now,
                        UpdateUserId = obj.UpdateUserId
                    });
                    _context.Session.CommitTransaction();
                }

                // Update sys_users_attachs in smarthealthcare
                if (!string.IsNullOrEmpty(obj.ImageFileOld))
                {
                    var imageFileArr = obj.ImageFileOld.Split(',');
                    if (imageFileArr.Length > 0)
                    {
                        var imageFileArrOld = _context.Query<UsersAttach>().Where(p => p.UserId == obj.UserId).Select(q => q.Path).ToList();

                        foreach (var item in imageFileArr)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                _context.Session.BeginTransaction();
                                if (!imageFileArrOld.Contains(item))
                                {
                                    _context.Update<UsersAttach>(g => g.UserId == obj.UserId && g.Path == item, a => new UsersAttach
                                    {
                                        IsDelete = true
                                    });
                                }
                                _context.Session.CommitTransaction();
                            }
                        }
                    }
                }

                _context.Session.BeginTransaction();
                var files = Request.Form.Files;
                if(obj.ImageFileOld!=null && obj.ImageFileOld !="")
                    _context.Update<UsersAttach>(ua => ua.UserId == obj.UserId && !obj.ImageFileOld.Contains(ua.Path), x => new UsersAttach()
                    {
                        IsDelete = true
                    });
                else
                {
                    _context.Update<UsersAttach>(ua => ua.UserId == obj.UserId, x => new UsersAttach()
                    {
                        IsDelete = true
                    });
                }

                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        var uniqueFileName = GetUniqueFileName(ConvertToUnSign(file.FileName));
                        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        var filePath = Path.Combine(uploads, uniqueFileName);
                        if (file.Name == "cmnd")
                        {
                            //_context.Update<UsersAttach>(ua => ua.UserId == obj.UserId && !ua.Type.Equals("1"), x => new UsersAttach()
                            //{
                            //    IsDelete = true
                            //});

                            _context.Insert(() => new UsersAttach
                            {
                                UserId = obj.UserId,
                                Path = "/uploads/" + uniqueFileName,
                                Type = "1",
                                CreateUserId = obj.CreateUserId,
                                CreateDate = DateTime.Now
                            });
                        }
                        else
                        {
                            if (obj.AccountType == 2)
                            {
                                //_context.Update<UsersAttach>(ua => ua.UserId == obj.UserId && !ua.Type.Equals("3"), x => new UsersAttach()
                                //{
                                //    IsDelete = true
                                //});

                                _context.Insert(() => new UsersAttach
                                {
                                    UserId = obj.UserId,
                                    Path = "/uploads/" + uniqueFileName,
                                    Type = "3",
                                    CreateUserId = obj.CreateUserId,
                                    CreateDate = DateTime.Now
                                });
                            }
                            else if (obj.AccountType == 3)
                            {
                                //_context.Update<UsersAttach>(ua => ua.UserId == obj.UserId && !ua.Type.Equals("4"), x => new UsersAttach()
                                //{
                                //    IsDelete = true
                                //});

                                _context.Insert(() => new UsersAttach
                                {
                                    UserId = obj.UserId,
                                    Path = "/uploads/" + uniqueFileName,
                                    Type = "4",
                                    CreateUserId = obj.CreateUserId,
                                    CreateDate = DateTime.Now
                                });
                            }
                            else
                            {
                                //_context.Update<UsersAttach>(ua => ua.UserId == obj.UserId && !ua.Type.Equals("2"), x => new UsersAttach()
                                //{
                                //    IsDelete = true
                                //});

                                _context.Insert(() => new UsersAttach
                                {
                                    UserId = obj.UserId,
                                    Path = "/uploads/" + uniqueFileName,
                                    Type = "2",
                                    CreateUserId = obj.CreateUserId,
                                    CreateDate = DateTime.Now
                                });
                            }
                        }
                        file.CopyTo(new FileStream(filePath, FileMode.Create));
                    }
                }
                _context.Session.CommitTransaction();

                return Json(new ActionResultDto());
            }
            catch (Exception e)
            {
                if (_contextmdmdb.Session.IsInTransaction)
                {
                    _contextmdmdb.Session.RollbackTransaction();
                }

                return StatusCode(500, _excep.Throw("Có lỗi xảy ra !", e.Message));
            }

        }


        #region oldCode
        //[HttpPost]
        //[Route("api/users")]
        //public IActionResult Create([FromBody]ResetPasswordViewModel obj)
        //{
        //    var user = _context.Query<ResetPassword>().Where(u => u.UserName == obj.UserName || u.Email == obj.Email || u.PhoneNumber == obj.PhoneNumber && u.IsDelete == false).FirstOrDefault();

        //    if (user == null)
        //    {
        //        return StatusCode(404, _excep.Throw("Khôi phục mật khẩu không thành công", "Tên đăng nhập, Email hoặc số điện thoại không tồn tại"));
        //    }

        //    try
        //    {
        //        _context.Session.BeginTransaction();

        //        _context.Update<UserSecret>(us => us.UserId == user.UserId, x => new UserSecret()
        //        {
        //            IsActive = false
        //        });

        //        _context.Insert<UserSecret>(new UserSecret()
        //        {
        //            UserId = user.UserId,
        //            SecretCode = obj.SecretCode,
        //            IsActive = true,
        //            IsDelete = false,
        //            CreateDate = DateTime.Now
        //        });

        //        _context.Session.CommitTransaction();
        //        if (string.IsNullOrEmpty(user.Email))
        //        {
        //            return StatusCode(406, _excep.Throw("Khôi phục mật khẩu không thành công", "Tài khoản này chưa có email liên kết"));
        //        }

        //        if (!SendMail(user.Email, obj.SecretCode, user.UserName))
        //        {
        //            return StatusCode(500, _excep.Throw("Có lỗi xảy ra khi gửi mã bí mật"));
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        if (_context.Session.IsInTransaction)
        //        {
        //            _context.Session.RollbackTransaction();
        //        }
        //        return StatusCode(500, _excep.Throw("Có lỗi xảy ra", e.Message));
        //    }
        //    return Json(new ActionResultDto());
        //}

        //[HttpPut]
        //[Route("api/users")]
        //public IActionResult Update([FromBody] ResetPasswordViewModel obj)
        //{
        //    var user = _contextmdmdb.Query<ResetPassword>().Where(u => u.UserName == obj.UserName || u.Email == obj.Email || u.PhoneNumber == obj.PhoneNumber && u.IsDelete==false).FirstOrDefault();
        //    var secret = _contextmdmdb.Query<UserSecret>().Where(us => us.SecretCode == obj.SecretCode && us.UserId == user.UserId && us.IsDelete == false).FirstOrDefault();

        //    if (user == null)
        //    {
        //        return StatusCode(404, _excep.Throw("Đổi mật khẩu không thành công", "Tên đăng nhập, Email hoặc số điện thoại không đúng"));
        //    }
        //    else if (obj.SecretCode != null)
        //    {
        //        if (secret == null)
        //        {
        //            return StatusCode(406, _excep.Throw("Đổi mật khẩu không thành công", "Mã bí mật không đúng"));
        //        }
        //        if (secret.IsActive == false)
        //        {
        //            return StatusCode(406, _excep.Throw("Đổi mật khẩu không thành công", "Mã bí mật không còn hiệu lực"));
        //        }
        //    }

        //    if (Utils.VerifyHashedPassword(user.Password, obj.Password))
        //    {
        //        return StatusCode(406, _excep.Throw("Đổi mật khẩu không thành công", "Mật khẩu mới không được trùng mật khẩu hiện tại"));
        //    }

        //    try
        //    {
        //        _contextmdmdb.Session.BeginTransaction();

        //        _contextmdmdb.Update<UserSecret>(us => us.Id == secret.Id, x => new UserSecret()
        //        {
        //            IsDelete = true,
        //            IsActive = false
        //        });

        //        _contextmdmdb.Update<ResetPassword>(u => u.UserId == user.UserId, x => new ResetPassword()
        //        {
        //            Password = Utils.HashPassword(obj.Password)
        //        });

        //        _contextmdmdb.Session.CommitTransaction();
        //    }
        //    catch (Exception e)
        //    {
        //        if (_contextmdmdb.Session.IsInTransaction)
        //        {
        //            _contextmdmdb.Session.RollbackTransaction();
        //        }
        //        return StatusCode(500, _excep.Throw("Có lỗi xảy ra", e.Message));
        //    }
        //    return Json(new ActionResultDto());
        //}
        #endregion

        [HttpPut]
        [Route("api/user-approved")]
        public IActionResult Approved([FromBody] UserInputViewModel obj)
        {
            var user = _context.Query<User>().Where(u => u.UserId == obj.UserId)?.FirstOrDefault();
            if (user == null)
            {
                return null;
            }
            int statusTmp = user.Status == 1 ? 2 : (user.Status == 2 ? 3 : 2);
            try
            {
                _context.Session.BeginTransaction();

                _context.Update<User>(us => us.UserId == obj.UserId, x => new User()
                {
                    Status = statusTmp
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

        [HttpPut]
        [Route("api/user-locked")]
        public IActionResult Locke([FromBody] UserViewModel obj)
        {
            var user = _contextmdmdb.Query<UserMDM>().Where(u => u.UserName == obj.UserName).FirstOrDefault();
            int statusTmp = user.Status == 1 ? 0 : 1;
            try
            {
                _contextmdmdb.Session.BeginTransaction();

                _contextmdmdb.Update<UserMDM>(us => us.UserName == obj.UserName, x => new UserMDM()
                {
                    Status = statusTmp
                });

                _contextmdmdb.Session.CommitTransaction();

                SendMailLockUser(user.Email, user.UserName, statusTmp);
            }
            catch (Exception e)
            {
                if (_contextmdmdb.Session.IsInTransaction)
                {
                    _contextmdmdb.Session.RollbackTransaction();
                }
                return StatusCode(500, _excep.Throw("Có lỗi xảy ra", e.Message));
            }
            return Json(new ActionResultDto());
        }

        [HttpDelete]
        [Route("api/users")]
        public IActionResult Delete(int id)
        {
            try
            {
                // Delete sys_users in smarthealthcare
                _context.Session.BeginTransaction();
                _context.Update<User>(g => g.UserId == id, a => new User
                {
                    IsDelete = true
                });
                _context.Session.CommitTransaction();

                // Delete sys_users_healthfacilities in smarthealthcare
                _context.Session.BeginTransaction();
                _context.Update<UserHealthFacilities>(g => g.UserId == id, a => new UserHealthFacilities
                {
                    IsDelete = true
                });
                _context.Session.CommitTransaction();

                // Delete sys_users_groups in mdm
                _contextmdmdb.Session.BeginTransaction();
                _contextmdmdb.Update<UserGroup>(g => g.UserId == id, a => new UserGroup
                {
                    IsDelete = true
                });
                _contextmdmdb.Session.CommitTransaction();

                // Delete sys_users in mdm
                _contextmdmdb.Session.BeginTransaction();
                _contextmdmdb.Update<UserMDM>(g => g.UserId == id, a => new UserMDM
                {
                    IsDelete = true
                });
                _contextmdmdb.Session.CommitTransaction();

                return Json(new ActionResultDto());
            }
            catch (Exception e)
            {
                if (_contextmdmdb.Session.IsInTransaction)
                {
                    _contextmdmdb.Session.RollbackTransaction();
                }

                return StatusCode(500, _excep.Throw(500, "Đã có lỗi xảy ra !", e.Message));
            }
        }

        [HttpPut]
        [Route("api/users-reset-password")]
        public ActionResult ResetPassword([FromBody]ResetPasswordViewModel obj)
        {
            var user = _contextmdmdb.Query<UserMDM>().Where(q => q.UserId == obj.UserId).FirstOrDefault();

            if (Utils.VerifyHashedPassword(user.Password, obj.Password))
            {
                return StatusCode(406, _excep.Throw("Đổi mật khẩu không thành công", "Mật khẩu mới không được trùng mật khẩu hiện tại"));
            }

            try
            {
                _contextmdmdb.Session.BeginTransaction();

                _contextmdmdb.Update<UserMDM>(u => u.UserId == obj.UserId, x => new UserMDM()
                {
                    Password = Utils.HashPassword(obj.Password)
                });

                _contextmdmdb.Session.CommitTransaction();

                SendMailUpdatePassword(user.Email, user.UserName, obj.Password);
            }
            catch (Exception e)
            {
                if (_contextmdmdb.Session.IsInTransaction)
                {
                    _contextmdmdb.Session.RollbackTransaction();
                }
                return StatusCode(500, _excep.Throw("Có lỗi xảy ra", e.Message));
            }
            return Json(new ActionResultDto());

        }

        public bool SendMailUpdatePassword(string sendTo, string user, string pass)
        {
            string userName = "configshc@gmail.com";
            string password = "Abc@123456";
            try
            {
                SmtpClient mailClient = new SmtpClient("smtp.gmail.com", 587);
                mailClient.EnableSsl = true;
                mailClient.Credentials = new NetworkCredential(userName, password);

                MailMessage message = new MailMessage(userName, sendTo);

                message.Subject = "[SHC] Thay đổi mật khẩu";

                message.Body = "Xin chào " + user + "\nChúng tôi vừa nhận được yêu cầu thay đổi mật khẩu từ phía bạn" + "\nMật khẩu mới của bạn là: " + pass + "\nĐể bảo mật, bạn vui lòng thay đổi mật khẩu sau khi đăng nhập và không tiết lộ cho bất kỳ cá nhân nào.";

                mailClient.Send(message);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SendMailLockUser(string sendTo, string user, int checkLock)
        {
            string userName = "configshc@gmail.com";
            string password = "Abc@123456";
            try
            {
                SmtpClient mailClient = new SmtpClient("smtp.gmail.com", 587);
                mailClient.EnableSsl = true;
                mailClient.Credentials = new NetworkCredential(userName, password);

                MailMessage message = new MailMessage(userName, sendTo);

                if (checkLock == 0)
                {
                    message.Subject = "[SHC] Thông báo khóa tài khoản";

                    message.Body = "Xin chào " + user + "\nChúng tôi nhận thấy có một số điều bất thường từ tài khoản của bạn. Do đó, tài khoản " + user + " tạm thời bị khóa.\nMọi chi tiết xin liên hệ admin của hệ thống qua 0982314938. Chân thành cảm ơn và xin lỗi vì sự bất tiện này";
                }
                else
                {
                    message.Subject = "[SHC] Thông báo mở khóa tài khoản";

                    message.Body = "Xin chào " + user + "\nChúng tôi nhận được yêu cầu kích hoạt lại tài khoản cho bạn. \nMọi chi tiết xin liên hệ 09764564123 để được tư vấn và giải đáp thắc mắc. Chúng tôi xin chân thành cảm ơn! ";
                }

                mailClient.Send(message);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SendMail(string sendTo, string newPassword, string user)
        {
            string userName = "configshc@gmail.com";
            string password = "Abc@123456";
            try
            {
                SmtpClient mailClient = new SmtpClient("smtp.gmail.com", 587);
                mailClient.EnableSsl = true;
                mailClient.Credentials = new NetworkCredential(userName, password);

                MailMessage message = new MailMessage(userName, sendTo);

                message.Subject = "[SHC] Thay đổi mật khẩu";

                message.Body = "Xin chào " + user + "\nChúng tôi vừa nhận được yêu cầu thay đổi mật khẩu từ phía bạn. Mật khẩu mới của bạn là: " + newPassword + "\nĐể bảo mật, bạn vui lòng thay đổi mật khẩu sau khi đăng nhập và không tiết lộ cho bất kỳ cá nhân nào.";

                mailClient.Send(message);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string ConvertToUnSign(string s)
        {
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = s.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D').Replace(" ", "_");
        }

        private string GetUniqueFileName(string fileName)
        {
            fileName = Path.GetFileName(fileName);
            return Path.GetFileNameWithoutExtension(fileName)
                      + "_"
                      + Guid.NewGuid().ToString().Substring(0, 4)
                      + Path.GetExtension(fileName);
        }

        [HttpPut]
        [Route("api/users")]
        public IActionResult UpdatePassword([FromBody]ResetPasswordViewModel obj)
        {
            var user = _contextmdmdb.Query<ResetPassword>().Where(u => (u.Email == obj.Email || u.PhoneNumber == obj.PhoneNumber) && u.IsDelete == false).FirstOrDefault();

            if (user == null)
            {
                return StatusCode(404, _excep.Throw("Khôi phục mật khẩu không thành công", "Số điện thoại hoặc Email không tồn tại"));
            }

            try
            {

                //_context.Update<UserSecret>(us => us.UserId == user.UserId, x => new UserSecret()
                //{
                //    IsActive=false
                //});

                //_context.Insert<UserSecret>(new UserSecret()
                //{
                //    UserId = user.UserId,
                //    SecretCode = obj.SecretCode,
                //    IsActive=true,
                //    IsDelete=false,
                //    CreateDate=DateTime.Now
                //});



                if (string.IsNullOrEmpty(user.Email))
                {
                    return StatusCode(406, _excep.Throw("Khôi phục mật khẩu không thành công", "Tài khoản này chưa có email liên kết"));
                }
                else if (string.IsNullOrEmpty(user.Email) && string.IsNullOrEmpty(user.PhoneNumber))
                {
                    return StatusCode(406, _excep.Throw("Khôi phục mật khẩu không thành công", "Tài khoản này chưa có email, số điện thoại liên kết"));
                }

                _contextmdmdb.Session.BeginTransaction();

                _contextmdmdb.Update<ResetPassword>(u => u.UserId == user.UserId, x => new ResetPassword()
                {
                    Password = Utils.HashPassword(GeneratorPassword()),
                    UpdateDate = DateTime.Now
                });

                _contextmdmdb.Session.CommitTransaction();

                if (!SendMail(user.Email, _newPassword, user.UserName))
                {
                    return StatusCode(500, _excep.Throw("Có lỗi xảy ra khi gửi lại mật khẩu"));
                }
            }
            catch (Exception e)
            {
                if (_contextmdmdb.Session.IsInTransaction)
                {
                    _contextmdmdb.Session.RollbackTransaction();
                }
                return StatusCode(500, _excep.Throw("Có lỗi xảy ra", e.Message));
            }
            return Json(new ActionResultDto());
        }
        #region oldCode
        //[HttpPut]
        //[Route("api/users")]
        //public IActionResult Update([FromBody] ResetPasswordViewModel obj)
        //{
        //    var user = _context.Query<ResetPassword>().Where(u => u.UserName == obj.UserName || u.Email == obj.Email || u.PhoneNumber == obj.PhoneNumber && u.IsDelete==false).FirstOrDefault();
        //    //var secret = _context.Query<UserSecret>().Where(us => us.SecretCode == obj.SecretCode && us.UserId == user.UserId && us.IsDelete == false).FirstOrDefault();

        //    if (user == null)
        //    {
        //        return StatusCode(404, _excep.Throw("Khôi phục mật khẩu không thành công", "Tên đăng nhập, Email hoặc số điện thoại không đúng"));
        //    }
        //    //else if (obj.SecretCode != null)
        //    //{
        //    //    if (secret == null)
        //    //    {
        //    //        return StatusCode(406, _excep.Throw("Khôi phục mật khẩu không thành công", "Mã bí mật không đúng"));
        //    //    }
        //    //    if (secret.IsActive == false)
        //    //    {
        //    //        return StatusCode(406, _excep.Throw("Khôi phục mật khẩu không thành công", "Mã bí mật không còn hiệu lực"));
        //    //    }
        //    //}

        //    //if (Utils.VerifyHashedPassword(user.Password, obj.Password))
        //    //{
        //    //    return StatusCode(406, _excep.Throw("Khôi phục mật khẩu không thành công", "Mật khẩu mới không được trùng mật khẩu hiện tại"));
        //    //}

        //    try
        //    {
        //        _context.Session.BeginTransaction();

        //        //_context.Update<UserSecret>(us => us.Id == secret.Id, x => new UserSecret()
        //        //{
        //        //    IsDelete = true,
        //        //    IsActive = false
        //        //});

        //        _context.Update<ResetPassword>(u => u.UserId == user.UserId, x => new ResetPassword()
        //        {
        //            Password = Utils.HashPassword(GeneratorPassword())
        //        });

        //        _context.Session.CommitTransaction();
        //    }
        //    catch (Exception e)
        //    {
        //        if (_context.Session.IsInTransaction)
        //        {
        //            _context.Session.RollbackTransaction();
        //        }
        //        return StatusCode(500, _excep.Throw("Có lỗi xảy ra", e.Message));
        //    }
        //    return Json(new ActionResultDto());
        //}
        #endregion
        public string GeneratorPassword()
        {
            string newPassword;
            bool includeLowercase = true;
            bool includeNumeric = true;
            bool includeSpecial = true;
            bool includeUppercase = true;
            int lengthOfPassword = 10;

            const int MAXIMUM_IDENTICAL_CONSECUTIVE_CHARS = 2;
            const string LOWERCASE_CHARACTERS = "abcdefghijklmnopqrstuvwxyz";
            const string UPPERCASE_CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string NUMERIC_CHARACTERS = "0123456789";
            const string SPECIAL_CHARACTERS = @"!#$%&*@\";

            //const int PASSWORD_LENGTH_MIN = 8;
            //const int PASSWORD_LENGTH_MAX = 128;

            //if (lengthOfPassword < PASSWORD_LENGTH_MIN || lengthOfPassword > PASSWORD_LENGTH_MAX)
            //{
            //    return "Password length must be between 8 and 128.";
            //}

            string characterSet = "";

            if (includeLowercase)
            {
                characterSet += LOWERCASE_CHARACTERS;
            }

            if (includeUppercase)
            {
                characterSet += UPPERCASE_CHARACTERS;
            }

            if (includeNumeric)
            {
                characterSet += NUMERIC_CHARACTERS;
            }

            if (includeSpecial)
            {
                characterSet += SPECIAL_CHARACTERS;
            }

            char[] password = new char[lengthOfPassword];
            int characterSetLength = characterSet.Length;

            System.Random random = new System.Random();

            for (int i = 0; i < 4; i++)
            {
                if (i == 0)
                    password[i] = LOWERCASE_CHARACTERS[random.Next(0, 25)];
                if (i == 1)
                    password[i] = UPPERCASE_CHARACTERS[random.Next(0, 25)];
                if (i == 2)
                    password[i] = NUMERIC_CHARACTERS[random.Next(0, 9)];
                if (i == 3)
                    password[i] = SPECIAL_CHARACTERS[random.Next(0, 6)];
            }

            for (int characterPosition = 4; characterPosition < lengthOfPassword; characterPosition++)
            {
                password[characterPosition] = characterSet[random.Next(characterSetLength - 1)];

                bool moreThanTwoIdenticalInARow =
                    characterPosition > MAXIMUM_IDENTICAL_CONSECUTIVE_CHARS
                    && password[characterPosition] == password[characterPosition - 1]
                    && password[characterPosition - 1] == password[characterPosition - 2];

                if (moreThanTwoIdenticalInARow)
                {
                    characterPosition--;
                }
            }

            _newPassword = newPassword = string.Join(null, password);
            return _newPassword;
        }
    }
}