﻿
using System.ComponentModel.DataAnnotations.Schema;
namespace Data.Entities
{
    public class FileDO : BaseDO
    {
        public int Id { get; set; }

        public string CloudStorageUrl { get; set; }

        public string OriginalFileName { get; set; }

        public string DockyardAccountID { get; set; }
    }
}
