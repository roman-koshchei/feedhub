using System.Diagnostics.CodeAnalysis;

namespace Web.Lib;

public class Res<T>
{
    public Exception? Err { get; set; }
    public T? Val { get; set; }

    public Res(T value)
    {
        Val = value;
        Err = null;
    }

    public Res(Exception? err)
    {
        Val = default;
        Err = err;
    }

    [MemberNotNullWhen(true, nameof(Err))]
    [MemberNotNullWhen(false, nameof(Val))]
    public bool IsErr => Err is not null;
}