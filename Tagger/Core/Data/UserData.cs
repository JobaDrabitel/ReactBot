using System.Threading;

namespace Tagger.Core.ViewModels
{
    internal class UserData
    {
        public string Phone { get; set; } = "";
        public int TaggedUsers { get; set; } = 0;
        public int SendedMessages { get; set; } = 0;
        public int FloodWait { get; set; }
    }
}
