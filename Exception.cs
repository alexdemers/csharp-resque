using System;

namespace Resque
{
    class ResqueException : Exception
    {
        public ResqueException(string message) : base(message)
        {
            
        }
    }
}
