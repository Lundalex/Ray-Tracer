
[System.Serializable]
public class Wrapper<T>
{
    public T[] array;
    public Wrapper(T[] array) => this.array = array;
}