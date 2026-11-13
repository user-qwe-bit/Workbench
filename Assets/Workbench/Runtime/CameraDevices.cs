using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace Workbench {
// DirectShow's native enumerator, matching OpenCV CAP_DSHOW order. Does not open devices.
// Explicit vtable calls avoid Mono's incomplete COM interface-array marshalling.
public static class CameraDevices {
 [DllImport("ole32.dll")]static extern int CoInitializeEx(IntPtr reserved,uint mode);
 [DllImport("ole32.dll")]static extern void CoUninitialize();
 [DllImport("ole32.dll")]static extern int CoCreateInstance(ref Guid clsid,IntPtr outer,uint context,ref Guid iid,out IntPtr result);
 [DllImport("oleaut32.dll")]static extern int VariantClear(IntPtr variant);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate uint Release(IntPtr self);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int CreateEnum(IntPtr self,ref Guid category,out IntPtr enumeration,uint flags);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Next(IntPtr self,uint count,out IntPtr moniker,out uint fetched);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Bind(IntPtr self,IntPtr context,IntPtr left,ref Guid iid,out IntPtr storage);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Read(IntPtr self,[MarshalAs(UnmanagedType.LPWStr)]string key,IntPtr value,IntPtr errorLog);
 static T Method<T>(IntPtr p,int slot) where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(p),slot*IntPtr.Size));
 static void Free(IntPtr p){if(p!=IntPtr.Zero)Method<Release>(p,2)(p);}
 public static string LastError{get;private set;}="";
 public static InputRecognition.Device[] Enumerate(){var result=new List<InputRecognition.Device>();IntPtr factory=IntPtr.Zero,enumerator=IntPtr.Zero;int initialized=CoInitializeEx(IntPtr.Zero,0);LastError="";
  try{var clsid=new Guid("62BE5D10-60EB-11D0-BD3B-00A0C911CE86");var iid=new Guid("29840822-5B84-11D0-BD3B-00A0C911CE86");int hr=CoCreateInstance(ref clsid,IntPtr.Zero,1,ref iid,out factory);if(hr<0)Marshal.ThrowExceptionForHR(hr);var category=new Guid("860BB310-5D01-11D0-BD3B-00A0C911CE86");hr=Method<CreateEnum>(factory,3)(factory,ref category,out enumerator,0);if(hr==1)return result.ToArray();if(hr<0)Marshal.ThrowExceptionForHR(hr);
   int index=0;while(Method<Next>(enumerator,3)(enumerator,1,out var moniker,out var fetched)==0&&fetched==1){IntPtr bag=IntPtr.Zero,variant=Marshal.AllocCoTaskMem(24);for(int i=0;i<24;i++)Marshal.WriteByte(variant,i,0);
    try{var bagId=new Guid("55272A00-42CB-11CE-8135-00AA004BB851");hr=Method<Bind>(moniker,9)(moniker,IntPtr.Zero,IntPtr.Zero,ref bagId,out bag);if(hr<0)Marshal.ThrowExceptionForHR(hr);hr=Method<Read>(bag,3)(bag,"FriendlyName",variant,IntPtr.Zero);if(hr==0&&Marshal.ReadInt16(variant)==8){string name=Marshal.PtrToStringBSTR(Marshal.ReadIntPtr(variant,8));result.Add(new InputRecognition.Device{index=index,name=name});}}
    finally{VariantClear(variant);Marshal.FreeCoTaskMem(variant);Free(bag);Free(moniker);index++;}
   }
  }catch(Exception e){LastError=e.Message;UnityEngine.Debug.LogWarning("Camera enumeration: "+e.Message);}finally{Free(enumerator);Free(factory);if(initialized>=0)CoUninitialize();}return result.ToArray();
 }
}
}
