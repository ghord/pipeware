using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware;

public delegate void SyncRequestDelegate<TRequestContext>(TRequestContext context) where TRequestContext : IRequestContext;
