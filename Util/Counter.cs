namespace Prem.Util
{
    public class Counter
    {
        private int _next_id = 0;

        public int AllocateId()
        {
            int id = _next_id;
            _next_id++;
            return id;
        }
    }
}