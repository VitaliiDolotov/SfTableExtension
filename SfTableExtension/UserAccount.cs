using System.Collections.Generic;

namespace SfTableExtension
{
    class UserAccount
    {
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                TruncatedName = value[..3];
            }
        }

        public string TruncatedName { get; set; }
        public string Phone { get; set; }

        //public List<string> Roles { get; set; }
        public IList<string> Roles;
        //public IEnumerable<string> Roles { get; set; }
        //public string[] Roles { get; set; }

        //public List<bool> RoleActivationStatus { get; set; }
        //public IList<bool> RoleActivationStatus { get; set; }
        //public IEnumerable<bool> RoleActivationStatus { get; set; }
        //public bool[] RoleActivationStatus { get; set; }
        public bool[] RoleActivationStatus;

        //public List<double> AccountScore { get; set; }
        //public IList<double> AccountScore { get; set; }
        //public IEnumerable<double> AccountScore { get; set; }
        public int[] AccountScore { get; set; }

        public List<string> Addresses;
        public List<string> AddressesDetails { get; set; }

        public List<Language> Languages { get; set; }
        //public IList<Language> Languages { get; set; }
        //public IEnumerable<Language> Languages { get; set; }
        //public Language[] Languages { get; set; }
        //public Language[] Languages;
    }

    public enum Language
    {
        English, Finnish, Swedish
    }
}
