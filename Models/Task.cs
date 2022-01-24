using System;

namespace Project.Models

{
    public class UserTask
    {
        public int id { get; set; }
        public DateTime date { get; set; }
        public string task { get; set; }
        public int progress { get; set; }
        public bool closed { get; set; }
        public long chatid { get; set; }
        public string user_name { get; set; }
    }
}
