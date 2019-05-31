﻿using System;
using Viettel.Annotations;
using Viettel.Entity;

namespace SHCServer.Models
{
    [Table("sys_users")]
    public class UserMDM //: IEntity
    {
        [Column(IsPrimaryKey = true)]
        [AutoIncrement]
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public int AccountType { get; set; }
        public int Sex { get; set; }
        public DateTime? BirthDay { get; set; }
        public DateTime? UpdateDate { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? Status { get; set; }

        // Location
        public string ProvinceCode { get; set; }
        public string DistrictCode { get; set; }
        public string WardCode { get; set; }
        public string Address { get; set; }

    }
}