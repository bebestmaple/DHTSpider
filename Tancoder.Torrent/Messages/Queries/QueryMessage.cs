#if !DISABLE_DHT
//
// QueryMessage.cs
//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
//
// Copyright (C) 2008 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using Tancoder.Torrent.BEncoding;

namespace Tancoder.Torrent.Dht.Messages
{
    public abstract class QueryMessage : DhtMessage
    {
        private static readonly BEncodedString QueryArgumentsKey = "a";
        private static readonly BEncodedString QueryNameKey = "q";
        public static readonly BEncodedString QueryType = "q";
        private ResponseCreator responseCreator;

        public override NodeId Id
        {
            get { return new NodeId((BEncodedString)Parameters[IdKey]); }
        }

        public ResponseCreator ResponseCreator
        {
            get { return responseCreator; }
            private set { responseCreator = value; }
        }

        public BEncodedDictionary Parameters
        {
            get { return (BEncodedDictionary)properties[QueryArgumentsKey]; }
        }

        protected QueryMessage(NodeId id, BEncodedString queryName, ResponseCreator responseCreator)
            : this(id, queryName, new BEncodedDictionary(), responseCreator)
        {

        }

        protected QueryMessage(NodeId id, BEncodedString queryName, BEncodedDictionary queryArguments, ResponseCreator responseCreator)
            : base(QueryType)
        {
            properties.Add(QueryNameKey, queryName);
            properties.Add(QueryArgumentsKey, queryArguments);

            Parameters.Add(IdKey, id.BencodedString());
            ResponseCreator = responseCreator;
        }

        protected QueryMessage(BEncodedDictionary d, ResponseCreator responseCreator)
            : base(d)
        {
            ResponseCreator = responseCreator;
        }
    }
}
#endif