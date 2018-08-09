using System;

namespace ModelManager.Types
{
    public class MemberOrderAttribute : Attribute
    {
        public double Order { get; private set; }

        public MemberOrderAttribute(double order)
        {
            Order = order;
        }
    }
}