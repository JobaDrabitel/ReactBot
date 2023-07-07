using System.Threading;

namespace Tagger.Core.ViewModels
{
    internal class GroupData
    {
        public string Group { get; set; } = "";
        public int SendedMessages { get; set; } = 0;
        public int SendedMessagesInCurrentCycle { get; set; } = 0;
        public int TaggedUsers { get; set; } = 0;
    }
}
