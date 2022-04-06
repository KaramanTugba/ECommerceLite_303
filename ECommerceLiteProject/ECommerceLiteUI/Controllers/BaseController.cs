﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ECommerceLiteUI.Controllers
{
    public class BaseController : Controller
    {
        [NonAction]
        public string CreateRandomNewPassword()
        {
            Random rnd = new Random();
            int number = rnd.Next(1000, 5000);
            char[] guidstring = Guid.NewGuid().ToString().Replace("-", "").ToArray();
            string newPassword = string.Empty;
            for (int i = 0; i < guidstring.Length; i++)
            {
                if (newPassword.Length == 5) break;
                if (char.IsLetter(guidstring[i]))
                {
                    newPassword += guidstring[i];
                }
            }
            newPassword += number;
            return newPassword;
        }
    }
}