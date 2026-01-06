namespace System_Pucharowy.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int Round { get; set; }

        public User Player1 { get; set; } = null!;
        public User Player2 { get; set; } = null!;
        public User? Winner { get; set; } 
    }
}
