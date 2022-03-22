﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityLayer.Concrete
{
    public class Student
    {
        [Key]
        public int StudentID { get; set; }
        [StringLength(20)]
        public string StudentName { get; set; }
        [StringLength(30)]
        public string StudentSurname { get; set; }
        [StringLength(11)]
        public string StudentPhone { get; set; }
        public DateTime  StudentBirthDate { get; set; }


        public int ClassroomID { get; set; }
        public virtual Classroom Classroom { get; set; }

        public int TeacherID { get; set; }
        public virtual Teacher Teacher { get; set; }

        public int LessonID { get; set; }
        public virtual Lesson Lesson { get; set; }

    }
}
