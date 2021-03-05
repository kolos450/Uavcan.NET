using Splat;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Uavcan.NET.Studio
{
    sealed class MefDependencyResolver : IDependencyResolver
    {
        readonly CompositionContainer _container;

        public MefDependencyResolver(CompositionContainer container)
        {
            _container = container;
        }

        public void Dispose()
        {
        }

        public object GetService(Type serviceType, string contract = null)
        {
            return GetServices(serviceType, contract)
                .FirstOrDefault();
        }

        public IEnumerable<object> GetServices(Type serviceType, string contract = null)
        {
            var exports = _container.GetExports(serviceType, null, contract);
            foreach (var ex in exports)
            {
                yield return ex.Value;
            }
        }

        public bool HasRegistration(Type serviceType, string contract = null) =>
            _container.GetExports(serviceType, null, contract).Any();

        public void Register(Func<object> factory, Type serviceType, string contract = null)
        {
            var batch = new CompositionBatch();
            if (contract == null)
                contract = AttributedModelServices.GetContractName(serviceType);
            var typeIdentity = AttributedModelServices.GetTypeIdentity(serviceType);
            var metadata = new Dictionary<string, object>
            {
                ["ExportTypeIdentity"] = typeIdentity,
            };
            batch.AddExport(new Export(contract, metadata, factory));
            _container.Compose(batch);
        }

        public IDisposable ServiceRegistrationCallback(Type serviceType, string contract, Action<IDisposable> callback)
        {
            throw new NotSupportedException("Method is not used by RxUI.");
        }

        public void UnregisterAll(Type serviceType, string contract = null)
        {
            throw new NotSupportedException("Method is not used by RxUI.");
        }

        public void UnregisterCurrent(Type serviceType, string contract = null)
        {
            throw new NotSupportedException("Method is not used by RxUI.");
        }
    }
}
