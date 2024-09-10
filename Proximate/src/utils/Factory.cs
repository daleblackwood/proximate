public interface IFactoryBuilder<TData, TObject> {

    bool CanBuild(TData data);

    TObject Build(TData data);

}

public interface IFactory<TData, TObject> {
    public void Add(params IFactoryBuilder<TData, TObject>[] builderParams);
    TObject Build(TData data);
}

public class Factory<TData, TObject> : IFactory<TData, TObject> {

    private List<IFactoryBuilder<TData, TObject>> builders = new();

    public void Add(params IFactoryBuilder<TData, TObject>[] builderParams) 
    {
        foreach (var builder in builderParams) 
        {
            builders.Add(builder);
        }
    }

    public virtual TObject Build(TData data) 
    {
        var builder = GetBuilder(data);
        if (builder == null)
            return default!;

        return builder.Build(data);
    }

    private IFactoryBuilder<TData, TObject>? GetBuilder(TData data)
    {
        foreach (var builder in builders)
        {
            if (builder.CanBuild(data))
            {
                return builder;
            }
        }
        return null;
    }

}