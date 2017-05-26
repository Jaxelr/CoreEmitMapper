﻿using System;
using System.IO;
using System.Reflection.Emit;
using System.Reflection;
using EmitMapper.Mappers;

namespace EmitMapper
{
	/// <summary>
	/// Class which maintains an assembly for created object Mappers
	/// </summary>
	public class DynamicAssemblyManager
	{
		/// <summary>
		/// Saves assembly with created Mappers to file. This method is useful for debugging purpose.
		/// </summary>
		public static void SaveAssembly()
		{
#if !SILVERLIGHT
			lock (typeof(DynamicAssemblyManager))
			{
				assemblyBuilder.Save(assemblyName.Name + ".dll");
			}
#else
		  throw new NotImplementedException("DynamicAssemblyManager.SaveAssembly");
#endif
		}

		#region Non-public members

		private static AssemblyName assemblyName;
		private static AssemblyBuilder assemblyBuilder;
		private static ModuleBuilder moduleBuilder;

		static DynamicAssemblyManager()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			
			StrongNameKeyPair kp = ExtractStrongNamePair(assembly);

#if !SILVERLIGHT
			assemblyName = new AssemblyName("EmitMapperAssembly");
			assemblyName.KeyPair = kp;
			assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				assemblyName,
				AssemblyBuilderAccess.RunAndSave
				);

			moduleBuilder = assemblyBuilder.DefineDynamicModule(
				assemblyName.Name,
				assemblyName.Name + ".dll",
				true);
#else
			assemblyName = new AssemblyName("EmitMapperAssembly.SL");
			assemblyName.KeyPair = kp;
			assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				  assemblyName,
				  AssemblyBuilderAccess.Run
				  );
			moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, true);
#endif
		}

		private static StrongNameKeyPair ExtractStrongNamePair(Assembly assembly)
		{
			string resourceName = string.Format("{0}.{1}", assembly.GetName().Name, "EmitMapper.snk");
			byte[] bytes;

			using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
			{
				int length = (int)resourceStream.Length;
				bytes = new byte[length];
				resourceStream.Read(bytes, 0, length);
			}

			return new StrongNameKeyPair(bytes);
		}

		private static string CorrectTypeName(string typeName)
		{
			if (typeName.Length >= 1042)
			{
				typeName = "type_" + typeName.Substring(0, 900) + Guid.NewGuid().ToString().Replace("-", "");
			}
			return typeName;
		}

		internal static TypeBuilder DefineMapperType(string typeName)
		{
			lock (typeof(DynamicAssemblyManager))
			{
				return moduleBuilder.DefineType(
					CorrectTypeName(typeName + Guid.NewGuid().ToString().Replace("-", "")),
					TypeAttributes.Public,
					typeof(MapperForClassImpl),
					null
					);
			}
		}

		internal static TypeBuilder DefineType(string typeName, Type parent)
		{
			lock (typeof(DynamicAssemblyManager))
			{
				return moduleBuilder.DefineType(
					CorrectTypeName(typeName),
					TypeAttributes.Public,
					parent,
					null
					);
			}
		}
		#endregion
	}
}