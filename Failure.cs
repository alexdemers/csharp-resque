using System;

namespace Resque
{
    public class Failure
    {
        private static Type _backend;

        public static Type Create(object payload, Exception exception, Worker worker, String queue)
        {
            Activator.CreateInstance(_backend, payload, exception, worker, queue);
            return _backend;
        }

        public static Type GetBackend()
        {
            return _backend ?? (_backend = typeof(Failures.Redis));
        }

        public static void SetBackend(Type backend)
        {
            _backend = backend;
        }
    }
}
