using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.Types
{
	public class MappedEntity
	{
		public string DatabaseName { get; set; }
		public string Schema { get; set; }
		public string TableName { get; set; }
		public Type EntityType { get; set; }
		public string Namespace
		{
			get { return EntityType?.Namespace; }
		}
		public string ObjectName
		{
			get { return EntityType?.Name; }
		}

		public object MapInstance { get; set; }

		public MappedEntity(object mapInstance)
		{
			EntityType = mapInstance.GetType().GetRuntimeProperties().First(c => c.Name == "ClrType").GetValue(mapInstance) as Type;
			MapInstance = mapInstance;
		}
	}
}
