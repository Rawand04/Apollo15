using System;
using System.Collections.Generic;

namespace SpaceSeries.Foundation.ServiceLocator
{
    /// <summary>Non-owning registry. The composition root owns service disposal.</summary>
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private bool _disposed;

        public void Register<T>(T service) where T : class
        {
            ThrowIfDisposed();
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (_services.ContainsKey(typeof(T)))
                throw new InvalidOperationException($"Service {typeof(T).FullName} is already registered.");
            _services.Add(typeof(T), service);
        }

        public T Resolve<T>() where T : class
        {
            ThrowIfDisposed();
            if (TryResolve<T>(out var service)) return service;
            throw new InvalidOperationException($"Service {typeof(T).FullName} is not registered.");
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            if (_disposed || !_services.TryGetValue(typeof(T), out var value)) return false;
            service = (T)value;
            return true;
        }

        public void Clear()
        {
            ThrowIfDisposed();
            _services.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _services.Clear();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceRegistry));
        }
    }
}
