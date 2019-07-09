using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uavcan.NET
{
    public sealed class ResponseTicket : IDisposable
    {
        SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);
        CancellationTokenSource _cts = new CancellationTokenSource();

        public TransferReceivedArgs Response { get; private set; }

        public void SetResponse(TransferReceivedArgs value)
        {
            Response = value;
            _semaphore.Release();
        }

        public async Task<TransferReceivedArgs> WaitForResponse(CancellationToken cancellationToken = default)
        {
            CancellationTokenSource cts = null;
            CancellationToken ct;
            if (cancellationToken == default)
            {
                ct = _cts.Token;
            }
            else
            {
                cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
                ct = cts.Token;
            }

            try
            {
                await _semaphore.WaitAsync(ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
            }
            finally
            {
                if (cts != null)
                    cts.Dispose();
            }

            return Response;
        }

        public void Cancel()
        {
            _cts.Cancel();
        }

        public void Dispose()
        {
            if (_cts != null)
            {
                _cts.Dispose();
                _cts = null;
            }

            if (_semaphore != null)
            {
                _semaphore.Dispose();
                _semaphore = null;
            }
        }
    }
}
