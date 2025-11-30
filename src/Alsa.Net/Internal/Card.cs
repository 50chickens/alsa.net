namespace Alsa.Net.Internal
{
    /// <summary>
    /// Represents an ALSA sound card.
    /// </summary>
    public class Card
    {
        private int _id;
        private string _name;

        /// <summary>
        /// Creates a new instance of the Card class.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public Card(int id, string name)
        {
            _id = id;
            _name = name;
        }
        /// <summary>
        /// Id of the sound card.
        /// </summary>
        public int Id => _id;
        /// <summary>
        /// Name of the sound card.
        /// </summary>
        public string Name => _name;
    }
}
