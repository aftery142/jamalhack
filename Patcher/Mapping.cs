using dnlib.DotNet;
using dnlib.DotNet.Emit;
using EazDecodeLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Patcher
{
    public class Mapping
    {
        private readonly Dictionary<string, string> fwd = new Dictionary<string, string>(),
            bck = new Dictionary<string, string>();
        private readonly CryptoHelper crypto = null;
        public Mapping(string key)
        {
            Console.WriteLine("[#] Using crypto key: " + key);
            crypto = new CryptoHelper(key);
        }
        public void Load(ModuleDefMD u)
        {
            foreach (TypeDef a in u.GetTypes())
            {
                Load(a);
                foreach (FieldDef b in a.Fields) Load(b);
                foreach (MethodDef b in a.Methods) Load(b);
            }
        }
        private void Load(IFullName u)
        {
            if (u == null || !u.Name.StartsWith("#=")) return;
            string v = crypto.Decrypt(u.Name);
            bck[u.Name] = v; fwd[v] = u.Name;
        }
        public void Remap(ModuleDefMD u)
        { //lol
            foreach (TypeDef a in u.GetTypes())
            {
                if (!a.FullName.StartsWith("Core")) continue;
                if (a.BaseType != null)
                    a.BaseType = Update(u, a.BaseType.ToTypeSig()).ToTypeDefOrRef();
                foreach (PropertyDef b in a.Properties)
                {
                    b.PropertySig.RetType = Update(u, b.PropertySig.RetType);
                    if (!b.HasCustomAttributes) continue;
                    for (int i = 0; i < b.CustomAttributes.Count; i++)
                    {
                        MemberRef d = (MemberRef)b.CustomAttributes[i].Constructor;
                        ITypeDefOrRef tmp = Update(u, d.DeclaringType.ToTypeSig()).ToTypeDefOrRef();
                        TypeDef t = tmp.ResolveTypeDef();
                        if ((t == null || t.FindMethod(d.Name) == null)
                            && !CanResolve(d.DeclaringType.ToTypeSig()))
                            d.Name = Encrypt(d.Name);
                        d.MethodSig.RetType = Update(u, d.MethodSig.RetType);
                        for (int j = 0; j < d.MethodSig.Params.Count; j++)
                            d.MethodSig.Params[j] = Update(u, d.MethodSig.Params[j]);
                        b.CustomAttributes[i].Constructor = new MemberRefUser(d.Module, d.Name, d.MethodSig, tmp);
                    }
                }
                foreach (FieldDef b in a.Fields)
                {
                    b.FieldSig.Type = Update(u, b.FieldSig.Type);
                    if (!b.HasCustomAttributes) continue;
                    for (int i = 0; i < b.CustomAttributes.Count; i++)
                    {
                        MemberRef d = (MemberRef)b.CustomAttributes[i].Constructor;
                        ITypeDefOrRef tmp = Update(u, d.DeclaringType.ToTypeSig()).ToTypeDefOrRef();
                        TypeDef t = tmp.ResolveTypeDef();
                        if ((t == null || t.FindMethod(d.Name) == null)
                            && !CanResolve(d.DeclaringType.ToTypeSig()))
                            d.Name = Encrypt(d.Name);
                        d.MethodSig.RetType = Update(u, d.MethodSig.RetType);
                        for (int j = 0; j < d.MethodSig.Params.Count; j++)
                            d.MethodSig.Params[j] = Update(u, d.MethodSig.Params[j]);
                        b.CustomAttributes[i].Constructor = new MemberRefUser(d.Module, d.Name, d.MethodSig, tmp);
                    }
                }
                foreach (MethodDef b in a.Methods)
                {
                    if (b.HasReturnType) b.ReturnType = Update(u, b.ReturnType);
                    if (!b.HasBody) continue;
                    foreach (Local c in b.Body.Variables) c.Type = Update(u, c.Type);
                    foreach (Parameter c in b.Parameters) c.Type = Update(u, c.Type);
                    foreach (Instruction c in b.Body.Instructions)
                    {
                        switch (c.Operand)
                        {
                            case MemberRef d:
                                if (d.IsFieldRef)
                                {
                                    if (!CanResolve(d.DeclaringType.ToTypeSig()))
                                        d.Name = Encrypt(d.Name);
                                    d.FieldSig.Type = Update(u, d.FieldSig.Type);
                                    c.Operand = new MemberRefUser(d.Module, d.Name, d.FieldSig,
                                        Update(u, d.DeclaringType.ToTypeSig()).ToTypeDefOrRef());
                                }
                                else
                                {
                                    ITypeDefOrRef tmp = Update(u, d.DeclaringType.ToTypeSig()).ToTypeDefOrRef();
                                    TypeDef t = tmp.ResolveTypeDef();
                                    if ((t == null || t.FindMethod(d.Name) == null) 
                                        && !CanResolve(d.DeclaringType.ToTypeSig()))
                                        d.Name = Encrypt(d.Name);
                                    d.MethodSig.RetType = Update(u, d.MethodSig.RetType);
                                    for (int i = 0; i < d.MethodSig.Params.Count; i++)
                                        d.MethodSig.Params[i] = Update(u, d.MethodSig.Params[i]);
                                    c.Operand = new MemberRefUser(d.Module, d.Name, d.MethodSig, tmp);
                                }
                                break;
                            case TypeRef d:
                                c.Operand = Update(u, d.ToTypeSig()).ToTypeDefOrRef();
                                break;
                            case TypeSpec d:
                                d.TypeSig = Update(u, d.TypeSig);
                                break;
                            case FieldDef d:
                                d.FieldSig.Type = Update(u, d.FieldSig.Type);
                                break;
                        }
                    }
                }
            }
        }
        private TypeSig Update(ModuleDefMD u, TypeSig sig)
        {
            if (sig.ElementType == ElementType.GenericInst)
                return Update(u, sig.ToGenericInstSig());
            else if (sig.ElementType == ElementType.Class || sig.ElementType == ElementType.ValueType)
                return CanResolve(sig) ? sig : FindType(u, sig).ToTypeSig();
            return sig;
        }
        public TypeSig Update(ModuleDefMD u, GenericInstSig sig)
        {
            if (!CanResolve(sig.GenericType))
                sig.GenericType = (ClassOrValueTypeSig)(FindType(u, sig.GenericType).ToTypeSig());
            for (int i = 0; i < sig.GenericArguments.Count; i++)
                sig.GenericArguments[i] = Update(u, sig.GenericArguments[i]);
            return sig;
        }
        private bool CanResolve(TypeSig sig)
        { //dunno what this is trust me bro
            return sig.IsCorLibType || sig.ElementType == ElementType.Var
                || sig.FullName.StartsWith("System") || sig.FullName.StartsWith("Core")
                || sig.FullName.StartsWith("<") || sig.FullName.StartsWith("Microsoft")
                || Type.GetType(sig.FullName) != null;
        }
        private TypeDef FindType(ModuleDefMD u, IFullName v)
            => u.Find(v.FullName, true) ?? u.Find(Encrypt(v.FullName), true);
        public string Encrypt(string s)
            => fwd.TryGetValue(s, out string tmp) ? tmp : s;
        public string Decrypt(string s)
            => bck.TryGetValue(s, out string tmp) ? tmp : s;
    }
}
