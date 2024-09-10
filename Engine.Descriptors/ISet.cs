namespace Engine;

// public delegate T SetGetterDelegate<T>(uint index);
//
// public interface ISet<T>
// {
//     T Get(uint index);
//     public T this [uint index] => Get(index);
// }
//
// public class InternalArraySet<T> : ISet<T>
// {
//     public T[] _data { get; set; }
//     
//     public InternalArraySet(T[] data) => _data = data;
//
//     public T Get(uint index) => throw new NotImplementedException();
// }
//
// public class InternalDelegateSet<T> : ISet<T>
// {
//     public SetGetterDelegate<T> _data { get; set; }
//     
//     public InternalDelegateSet(SetGetterDelegate<T> getter) => _data = getter;
//
//     public T Get(uint index) => _data(index);
// }
