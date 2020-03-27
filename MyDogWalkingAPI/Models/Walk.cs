using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDogWalkingAPI.Models
{
    public class Walk
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Duration { get; set; }
        public int WalkerId { get; set; }
        public int DogId { get; set; }
    }
}
