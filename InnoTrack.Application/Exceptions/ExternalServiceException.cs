using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Exceptions
{
    public class ExternalServiceException : AppException
    {
        public ExternalServiceException(string message) : base(message, 502)
        {
        }
    }
}
