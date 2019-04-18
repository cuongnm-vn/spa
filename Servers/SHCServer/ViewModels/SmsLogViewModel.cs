﻿using System;
using System.Collections.Generic;
using SHCServer.Models;
using Viettel;
using Viettel.MySql;

namespace SHCServer.ViewModels
{
    public class SmsLogViewModel
    {
        protected DbContext context;

        public SmsLogViewModel()
        {
        }

        public SmsLogViewModel(SmsLogs obj, string connectionString) : this()
        {
            context = new MySqlContext(new MySqlConnectionFactory(connectionString));

            Id = obj.Id;
            HealthFacilitiesId = obj.HealthFacilitiesId;
            SmsTemplateId = obj.SmsTemplateId;
            PhoneNumber = obj.PhoneNumber;
            Message = obj.Message;
            Status = obj.Status;
            SentDate = obj.SentDate;
            LogType = obj.LogType;
            Telco = obj.Telco;
            

            var healthFacilities = context.JoinQuery<SmsLogs, HealthFacilities>((s, h) => new object[]
                {
                    JoinType.InnerJoin, s.HealthFacilitiesId == h.HealthFacilitiesId
                })
                .Where((s, h) => s.Id == obj.Id)
                .Select((s, h) => h).FirstOrDefault();

            healthfacilitiesName = healthFacilities != null ? healthFacilities.Name : "";
        }

        public int Id { set; get; }
        public int HealthFacilitiesId { set; get; }
        public int SmsTemplateId { set; get; }
        public string PhoneNumber { set; get; }
        public string Message { set; get; }
        public int? Status { set; get; }
        public DateTime SentDate { set; get; }
        public int? LogType { set; get; }
        public string Telco { set; get; }
        public string healthfacilitiesName { set; get; }
    }
}