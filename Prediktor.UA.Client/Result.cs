using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace Prediktor.UA.Client
{
	public class Result<T>
	{
		public Result(T value)
		{
			Value = value;
		}

		public Result(StatusCode statusCode, string error)
		{
			StatusCode = statusCode;
			Error = error;
			Success = false;
		}

		public T Value { get; }

		public bool Success { get; } = true;
		public StatusCode StatusCode { get; } = 0;
		public string Error { get; }
	}
}
