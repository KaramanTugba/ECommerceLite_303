﻿using Microsoft.AspNet.Identity.EntityFramework;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceLiteEntity.IdentityModels
{
    public class ApplicationRole:IdentityRole
    {
        [StringLength(200)]
        public string Description { get; set; }
    }
}