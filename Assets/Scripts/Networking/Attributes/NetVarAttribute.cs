using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class NetVarAttribute : Attribute
{
	internal string m_callback;

	public NetVarAttribute()
	{
		m_callback = null;
	}

	public NetVarAttribute(string callback)
	{
		m_callback = callback;
	}
}