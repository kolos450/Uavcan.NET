using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uavcan.NET.Studio.DataTypes.Protocol.Param;

namespace Uavcan.NET.Studio.Communication
{
    public sealed record ParameterDescriptor(
        ushort Index,
        string Name,
        Value Value,
        Value DefaultValue,
        NumericValue MinValue,
        NumericValue MaxValue);

    public sealed class ParametersAccessor
    {
        static readonly Encoding Encoding = Encoding.UTF8;

        private readonly UavcanInstance _uavcan;
        private readonly NodeHandle _handle;

        public ParametersAccessor(UavcanInstance uavcan, NodeHandle handle)
        {
            _uavcan = uavcan;
            _handle = handle;
        }

        public async Task<Value> Get(string parameterName, CancellationToken ct = default)
        {
            var request = new GetSet_Request
            {
                Name = Encoding.GetBytes(parameterName),
                Value = new() { Empty = new() },
            };

            var response = await _uavcan.SendServiceRequest(_handle.NodeId, request, ct: ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            var responseData = _uavcan.Serializer.Deserialize<GetSet_Response>(response.ContentBytes);
            return responseData.Value;
        }

        public async Task<T> Get<T>(string parameterName, CancellationToken ct = default)
        {
            var value = await Get(parameterName, ct).ConfigureAwait(false);
            return Type.GetTypeCode(typeof(T).GetType()) switch
            {
                TypeCode.Byte => (T)(object)(byte)value.IntegerValue.Value,
                TypeCode.SByte => (T)(object)(sbyte)value.IntegerValue.Value,
                TypeCode.Int16 => (T)(object)(short)value.IntegerValue.Value,
                TypeCode.UInt16 => (T)(object)(ushort)value.IntegerValue.Value,
                TypeCode.Int32 => (T)(object)(int)value.IntegerValue.Value,
                TypeCode.UInt32 => (T)(object)(uint)value.IntegerValue.Value,
                TypeCode.Int64 => (T)(object)value.IntegerValue.Value,
                TypeCode.UInt64 => (T)(object)(ulong)value.IntegerValue.Value,
                TypeCode.Single => (T)(object)value.RealValue.Value,
                TypeCode.String => (T)(object)Encoding.GetString(value.StringValue),
                TypeCode.Boolean => (T)(object)(value.BooleanValue != 0),
                _ => throw new ArgumentOutOfRangeException(nameof(T)),
            };
        }

        public async IAsyncEnumerable<ParameterDescriptor> GetParameters([EnumeratorCancellation] CancellationToken ct = default)
        {
            var request = new GetSet_Request()
            {
                Value = new Value { Empty = new() }
            };

            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                request.Index = i;
                var response = await _uavcan.SendServiceRequest(_handle.NodeId, request, ct: ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                var responseData = _uavcan.Serializer.Deserialize<GetSet_Response>(response.ContentBytes);

                var nameBytes = responseData.Name;
                if (nameBytes is null || nameBytes.Length is 0)
                    yield break;

                yield return new ParameterDescriptor(
                    i,
                    Encoding.GetString(nameBytes),
                    responseData.Value,
                    responseData.DefaultValue,
                    responseData.MinValue,
                    responseData.MaxValue);
            }
        }

        public Task Set(string parameterName, long value, CancellationToken ct = default) =>
            Set(parameterName, r => r.Value.IntegerValue = value, ct);

        public Task Set(string parameterName, float value, CancellationToken ct = default) =>
            Set(parameterName, r => r.Value.RealValue = value, ct);

        public Task Set(string parameterName, bool value, CancellationToken ct = default) =>
            Set(parameterName, r => r.Value.BooleanValue = (byte)(value ? 1 : 0), ct);

        public Task Set(string parameterName, string value, CancellationToken ct = default) =>
            Set(parameterName, r => r.Value.StringValue = Encoding.GetBytes(value), ct);

        public async Task Set(string parameterName, Action<GetSet_Request> modifier, CancellationToken ct = default)
        {
            var request = new GetSet_Request
            {
                Name = Encoding.GetBytes(parameterName),
                Value = new()
            };
            modifier(request);

            var response = await _uavcan.SendServiceRequest(_handle.NodeId, request, ct: ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            var responseData = _uavcan.Serializer.Deserialize<GetSet_Response>(response.ContentBytes);

            if (!request.Value.Equals(responseData.Value))
            {
                throw new ParameterSetException("Cannot set remote node parameter.")
                {
                    LocalValue = request.Value,
                    RemoteValue = responseData.Value
                };
            }
        }
    }
}
