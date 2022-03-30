﻿using DataAccessLayer.Concrete.Repositories;
using EntityLayer.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Concrete
{
    public class WriterManager
    {
        GenericRepository<Writer> repo = new GenericRepository<Writer>();

        public List<Writer> GetAll()
        {
            return repo.List();
        }
        public void WriterAddBL(Writer p)
        {
            if (p.WriterName == "" || p.WriterSurname == "" || p.WriterMail == "" || p.Password == "")
            {
                //hata mesajı
            }
            else { repo.Insert(p); }
        }
    }
}