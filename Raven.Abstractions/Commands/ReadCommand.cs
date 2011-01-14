using System;
using System.Collections.Generic;
using Raven.Abstractions.Data;

namespace Raven.Abstractions.Commands
{
    public class ReadCommand : ICommand
    {
        public ReadRequest ReadRequest { get; set; }
        public ReadResults Result { get; set; }

        public ReadCommand()
        {
            ReadRequest = new ReadRequest();
        }

    	public string ArgumentsForLog
    	{
			get { return ReadRequest.Queue; }
    	}

    	public CommandType Type
        {
            get { return CommandType.Read; }
        }
    }
}