﻿using System.Collections.Generic;

namespace SfTableExtension
{
    class UserAccount
    {
        public string Name { get; set; }
        public string Phone { get; set; }

        //public List<string> Roles { get; set; }
        //public IList<string> Roles { get; set; }
        //public IEnumerable<string> Roles { get; set; }
        //public string[] Roles { get; set; }

        //public List<bool> RoleActivationStatus { get; set; }
        //public IList<bool> RoleActivationStatus { get; set; }
        //public IEnumerable<bool> RoleActivationStatus { get; set; }
        public bool[] RoleActivationStatus { get; set; }

        //public List<double> AccountScore { get; set; }
        //public IList<double> AccountScore { get; set; }
        //public IEnumerable<double> AccountScore { get; set; }
        public double[] AccountScore { get; set; }

        public List<string> Addresses { get; set; }

        //public List<Language> Language { get; set; }
    }

    public enum Language
    {
        English, Finnish, Swedish
    }
}