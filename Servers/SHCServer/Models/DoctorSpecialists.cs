﻿using System;
using System.Collections.Generic;
using SHCServer.ViewModels;
using Viettel;
using Viettel.Annotations;
using Viettel.Entity;

namespace SHCServer.Models
{
    [Table("cats_doctors_specialists")]
    public class DoctorSpecialists //: IEntity
    {
        public DoctorSpecialists()
        {

        }

        [Column(IsPrimaryKey = true)]
        [AutoIncrement]
        public int Id { get; set; }
        public int DoctorId { set; get; }
        public string SpecialistCode { set; get; }
        public bool? IsDelete { get; set; }
        public bool IsActive { get; set; }
    }
}