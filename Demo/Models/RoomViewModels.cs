using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Demo.Models
{
    public class RoomListItemViewModel
    {
        public string RoomId { get; set; }
        public string TypeId { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }

    public class RoomCreateEditViewModel
    {
        [Required, MaxLength(5)]
        [Display(Name = "Room ID")]
        public string RoomId { get; set; }

        [Required]
        [Display(Name = "Room Type")]
        public string TypeId { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        // New: file uploads
        public List<IFormFile> UploadedImages { get; set; } = new();

        // For display existing images
        public List<string> ExistingImages { get; set; } = new();

        // For dropdown
        public List<SelectListItem> RoomTypeItems { get; set; } = new();
    }
}
