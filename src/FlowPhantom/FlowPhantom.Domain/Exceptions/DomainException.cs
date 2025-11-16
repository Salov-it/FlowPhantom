using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Domain.Exceptions
{
    /// <summary>
    /// Базовое исключение доменного уровня.
    /// Любые ошибки доменной логики должны наследоваться от него.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}
