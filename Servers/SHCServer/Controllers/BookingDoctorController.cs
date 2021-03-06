﻿using System;
using System.Collections.Generic;
using SHCServer.Models;
using SHCServer.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Viettel;
using Viettel.MySql;
using System.Linq;

namespace SHCServer.Controllers
{
    public class BookingDoctorController : BaseController
    {
        private readonly string _connectionString;

        public BookingDoctorController(IOptions<Audience> settings, IConfiguration configuration)
        {
            _settings = settings;
            _context = new MySqlContext(new MySqlConnectionFactory(configuration.GetConnectionString("DefaultConnection")));

            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _excep = new FriendlyException();
        }

        [HttpGet]
        [Route("api/bookingdoctor")]
        public IActionResult GetAll(int skipCount = 0, int maxResultCount = 10, string sorting = null, string filter = null)
        {
            var objs = _context
                .JoinQuery<BookingDoctorsCalendars, BookingTimeslots>((dc, t) => new object[] { JoinType.InnerJoin, dc.TimeSlotId == t.TimeSlotId })
                .Where((dc, t) => dc.IsDelete == false && dc.IsActive == true && dc.CalendarDate.Year == DateTime.Now.Year);

            if (filter != null)
            {
                foreach (var (key, value) in JsonConvert.DeserializeObject<Dictionary<string, string>>(filter))
                {
                    if (string.IsNullOrEmpty(value)) continue;
                    if (string.Equals(key, "healthfacilities")) objs = objs.Where((dc, t) => dc.HealthFacilitiesId == int.Parse(value));
                    if (string.Equals(key, "doctor")) objs = objs.Where((dc, t) => dc.DoctorId == int.Parse(value));
                    if (string.Equals(key, "month")) objs = objs.Where((dc, t) => dc.CalendarDate.Month == int.Parse(value));
                    if (string.Equals(key, "status") && value != "3") objs = objs.Where((dc, t) => dc.Status == int.Parse(value));
                }
            }

            return Json(new ActionResultDto { Result = new { Items = objs.Select((dc, t) => new BookingDoctorsApproveViewModel(dc, t)).ToList() } });
        }

        [HttpPost]
        [Route("api/bookingdoctor")]
        public IActionResult Create([FromBody] BookingDoctorsInputViewModel bookingdoctor)
        {
            List<BookingDoctorsCalendars> lstDoctorCalendar = new List<BookingDoctorsCalendars>();

            /*foreach (TimeSlotInputViewModel el in bookingdoctor.LstTimeSlot)
            {
                BookingDoctorsCalendars doctorCalendar = new BookingDoctorsCalendars();
                doctorCalendar.Address = bookingdoctor.Address;
                doctorCalendar.HealthFacilitiesId = bookingdoctor.Healthfacilities;
                doctorCalendar.Status = bookingdoctor.Status;
                doctorCalendar.IsActive = true;
                doctorCalendar.DoctorId = bookingdoctor.Doctor;
                doctorCalendar.TimeSlotId = el.TimeSlotId;
                doctorCalendar.CalendarDate = DateTime.Parse(el.DateTime);

                doctorCalendar.CreateDate = DateTime.Now;
                doctorCalendar.CreateUserId = bookingdoctor.UserId;
                doctorCalendar.UpdateDate = DateTime.Now;
                doctorCalendar.UpdateUserId = bookingdoctor.UserId;

                lstDoctorCalendar.Add(doctorCalendar);
            }

            try
            {
                _context.InsertRange(lstDoctorCalendar);
                return Json(new ActionResultDto());
            }
            catch (Exception e)
            {
                return Json(new ActionResultDto { Error = e.Message });
            }*/

            try
            {
                var listTimeSlot = bookingdoctor.LstTimeSlot;
                foreach (TimeSlotInputViewModel el in listTimeSlot)
                {
                    DateTime calendarDate = DateTime.Parse(el.DateTime);
                    var bookingDoctorsCalendars = _context.Query<BookingDoctorsCalendars>().Where(b => b.DoctorId == bookingdoctor.Doctor && b.TimeSlotId == el.TimeSlotId && b.CalendarDate == calendarDate).FirstOrDefault();
                    if (bookingDoctorsCalendars == null)
                    {
                        _context.Session.BeginTransaction();
                        string subQuery = "INSERT INTO booking_doctors_calendars (Address, HealthFacilitiesId, Status, IsActive, DoctorId, TimeSlotId, CalendarDate, CreateUserId) VALUES ";
                        var subParamQuery = new List<string>();
                        var subParam = new List<DbParam>();

                        subParamQuery.Add($"(@Address, @HealthFacilitiesId, @Status, @IsActive, @DoctorId, @TimeSlotId, @CalendarDate, @CreateUserId)");
                        subParam.Add(DbParam.Create("@Address", bookingdoctor.Address));
                        subParam.Add(DbParam.Create("@HealthFacilitiesId", bookingdoctor.Healthfacilities));
                        subParam.Add(DbParam.Create("@Status", bookingdoctor.Status));
                        subParam.Add(DbParam.Create("@IsActive", true));
                        subParam.Add(DbParam.Create("@DoctorId", bookingdoctor.Doctor));
                        subParam.Add(DbParam.Create("@TimeSlotId", el.TimeSlotId));
                        subParam.Add(DbParam.Create("@CalendarDate", calendarDate));
                        subParam.Add(DbParam.Create("@CreateUserId", bookingdoctor.UserId));
                        _context.Session.ExecuteNonQuery($"{subQuery} {string.Join(",", subParamQuery)}", subParam);
                        _context.Session.CommitTransaction();
                    }
                    else
                    {
                        _context.Session.BeginTransaction();
                        string subQuery = "UPDATE booking_doctors_calendars SET Address = @Address, HealthFacilitiesId = @HealthFacilitiesId, Status = @Status, IsActive = @IsActive, UpdateDate = @UpdateDate, UpdateUserId = @CreateUserId WHERE DoctorId = @DoctorId AND TimeSlotId = @TimeSlotId AND CalendarDate = @CalendarDate";

                        var subParam = new List<DbParam>();
                        subParam.Add(DbParam.Create("@Address", bookingdoctor.Address));
                        subParam.Add(DbParam.Create("@HealthFacilitiesId", bookingdoctor.Healthfacilities));
                        subParam.Add(DbParam.Create("@Status", bookingdoctor.Status));
                        subParam.Add(DbParam.Create("@IsActive", true));
                        subParam.Add(DbParam.Create("@DoctorId", bookingdoctor.Doctor));
                        subParam.Add(DbParam.Create("@TimeSlotId", el.TimeSlotId));
                        subParam.Add(DbParam.Create("@CalendarDate", calendarDate));
                        subParam.Add(DbParam.Create("@UpdateDate", DateTime.Now));
                        subParam.Add(DbParam.Create("@CreateUserId", bookingdoctor.UserId));
                        _context.Session.ExecuteNonQuery($"{subQuery}", subParam);
                        _context.Session.CommitTransaction();
                    }
                }
                return Json(new ActionResultDto());
            }
            catch (Exception e)
            {
                return Json(new ActionResultDto { Error = e.Message });
            }

        }

        [HttpGet]
        [Route("api/bookingtimeslot")]
        public IActionResult GetBookingTimesSlot(int skipCount = 0, int maxResultCount = 10, string sorting = null, string filter = null)
        {
            int doctorId = 0;
            var objs = _context
                //.JoinQuery<BookingTimeslots, BookingDoctorsCalendars> ((t, dc) => new object[] { JoinType.InnerJoin, t.TimeSlotId == dc.TimeSlotId })
                .Query<BookingTimeslots>()
                .Where(t => t.IsDelete == false && t.IsActive == true);

            if (filter != null)
            {
                foreach (var (key, value) in JsonConvert.DeserializeObject<Dictionary<string, string>>(filter))
                {
                    if (string.IsNullOrEmpty(value)) continue;
                    if (string.Equals(key, "healthfacilities")) objs = objs.Where(t => t.HealthFacilitiesId == int.Parse(value));
                    if (string.Equals(key, "doctorId")) doctorId = int.Parse(value);
                }
            }

            return Json(new ActionResultDto { Result = new { Items = objs.Select(t => new BookingDoctorsViewModel(t, doctorId, _connectionString)).ToList() } });
        }

    }
}