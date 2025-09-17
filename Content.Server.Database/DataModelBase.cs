using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

//Starlight file
public abstract class DataModelBase
{
    public virtual void OnModelCreating(ServerDbContext dbContext, ModelBuilder modelBuilder)
    {
    }

}

public abstract partial class ServerDbContext
{
    private List<System.Type> ActiveDataModelTypes = new();
    private List<DataModelBase> ActiveDataModels = new();

    private void RegisterDataModel<T>() where T : DataModelBase, new()
    {
        if (ActiveDataModelTypes.Contains(typeof(T)))
            return;
        ActiveDataModelTypes.Add(typeof(T));
        ActiveDataModels.Add(new T());
    }

    private void RelayOnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var dataModel in ActiveDataModels)
        {
            dataModel.OnModelCreating(this, modelBuilder);
        }
    }

}