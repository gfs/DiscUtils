//
// Copyright (c) 2008-2011, Kenneth Bell
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

namespace DiscUtils.Nfs;

using System.Runtime.Serialization;
using System.Security.Permissions;

/// <summary>
/// Exception thrown when some invalid file system data is found, indicating probably corruption.
/// </summary>
[Serializable]
public sealed class RpcException : IOException
{
    /// <summary>
    /// Initializes a new instance of the RpcException class.
    /// </summary>
    public RpcException() {}

    /// <summary>
    /// Initializes a new instance of the RpcException class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public RpcException(string message)
        : base(message) {}

    /// <summary>
    /// Initializes a new instance of the RpcException class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RpcException(string message, Exception innerException)
        : base(message, innerException) {}

    /// <summary>
    /// Initializes a new instance of the RpcException class.
    /// </summary>
    /// <param name="reply">The RPC reply from the server.</param>
    internal RpcException(RpcReplyHeader reply)
        : base(GenerateMessage(reply)) {}

    /// <summary>
    /// Initializes a new instance of the RpcException class.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
#if !NETCOREAPP
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
#elif NET8_0_OR_GREATER
    [Obsolete]
#endif
    private RpcException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    private static string GenerateMessage(RpcReplyHeader reply)
    {
        if (reply.Status == RpcReplyStatus.Accepted)
        {
            switch (reply.AcceptReply.AcceptStatus)
            {
                case RpcAcceptStatus.Success:
                    return "RPC success";
                case RpcAcceptStatus.ProgramUnavailable:
                    return "RPC program unavailable";
                case RpcAcceptStatus.ProgramVersionMismatch:
                    if (reply.AcceptReply.MismatchInfo.Low == reply.AcceptReply.MismatchInfo.High)
                    {
                        return $"RPC program version mismatch, server supports version {reply.AcceptReply.MismatchInfo.Low}";
                    }
                    return $"RPC program version mismatch, server supports versions {reply.AcceptReply.MismatchInfo.Low} through {reply.AcceptReply.MismatchInfo.High}";

                case RpcAcceptStatus.ProcedureUnavailable:
                    return "RPC procedure unavailable";
                case RpcAcceptStatus.GarbageArguments:
                    return "RPC corrupt procedure arguments";
                default:
                    return "RPC failure";
            }
        }
        if (reply.RejectedReply.Status == RpcRejectedStatus.AuthError)
        {
            return reply.RejectedReply.AuthenticationStatus switch
            {
                RpcAuthenticationStatus.BadCredentials => "RPC authentication credentials bad",
                RpcAuthenticationStatus.RejectedCredentials => "RPC rejected authentication credentials",
                RpcAuthenticationStatus.BadVerifier => "RPC bad authentication verifier",
                RpcAuthenticationStatus.RejectedVerifier => "RPC rejected authentication verifier",
                RpcAuthenticationStatus.TooWeak => "RPC authentication credentials too weak",
                _ => "RPC authentication failure",
            };
        }
        if (reply.RejectedReply.MismatchInfo != null)
        {
            return $"RPC protocol version mismatch, server supports versions {reply.RejectedReply.MismatchInfo.Low} through {reply.RejectedReply.MismatchInfo.High}";
        }
        return "RPC protocol version mismatch, server didn't indicate supported versions";
    }
}