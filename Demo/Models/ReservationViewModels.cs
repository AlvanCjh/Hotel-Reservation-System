using System;
using System.Collections.Generic;

namespace Demo.Models
{
    public class ReservationListItemViewModel
    {
        public string ReservationId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string RoomId { get; set; }
        public string RoomTypeName { get; set; }
        public string RoomDescription { get; set; }
        public string RoomImage { get; set; }
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }

    public class ReservationDetailViewModel
    {
        public string ReservationId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string RoomId { get; set; }
        public string RoomTypeName { get; set; }
        public string RoomDescription { get; set; }
        public string RoomImage { get; set; }
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }

    public class MyBookingsViewModel
    {
        public List<ReservationListItemViewModel> UpcomingReservations { get; set; }
        public List<ReservationListItemViewModel> PastReservations { get; set; }
    }

}
