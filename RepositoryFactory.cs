using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Factory
{

    #region Repository Factory Test

    public static class RepositoryFactoryTest
    {
        public static void Exec()
        {
            IUnitOfWork unitOfWork = new UnitOfWorkEF();

            var customerRepository = unitOfWork.GetRepository<ICustomerRepository>();

            customerRepository.Create(new Customer());

            unitOfWork.Commit();

            var employeeRepository = unitOfWork.GetRepository<IEmployeeRepository>();

            var employees = employeeRepository.FindAll();

            var otherCustomerRepository = new UnitOfWorkEF().GetRepository<ICustomerRepository>();

            var equalInstances = otherCustomerRepository.Equals(customerRepository);

            if (!equalInstances)
                throw new Exception("Oops! Something went wrong, the instances should be the same.");
        }
    }


    #endregion

    #region Factory

    public static class RepositoryFactory
    {
        public static Dictionary<string, IRepositoryBase> GetRepositories<T>() where T : IRepositoryBase
        {
            return new Dictionary<string, IRepositoryBase>(
                         from type in Assembly.GetAssembly(typeof(T)).GetTypes()
                         let baseType = type.BaseType
                         where
                            type.IsClass
                         && !type.IsAbstract
                         && baseType != null
                         && baseType.IsGenericType
                         && baseType.GetGenericTypeDefinition() == typeof(RepositoryEF<>)
                         select new KeyValuePair<string, IRepositoryBase>(type.GetInterfaces().LastOrDefault().Name, (IRepositoryBase)Activator.CreateInstance(type)));
        }
    }

    #endregion

    #region UoW
    public interface IUnitOfWork
    {
        bool Commit();
        T GetRepository<T>() where T : IRepositoryBase;
    }

    public class UnitOfWorkEF : UnitOfWork
    {
        private static ContextEF context;
        public override bool Commit()
        {
            var committed = context.Commit();
            context.Dispose();
            return committed;
        }

        public override T GetRepository<T>()
        {
            if (context == null)
                context = new ContextEF();

            var repository = base.GetRepository<T>();
            ((IRepositorysEF)repository).SetContext(context);
            return repository;
        }
    }

    public abstract class UnitOfWork : IUnitOfWork
    {
        public abstract bool Commit();

        private static Dictionary<string, IRepositoryBase> repositories;

        private static readonly object mutex = new object();

        public virtual T GetRepository<T>() where T : IRepositoryBase
        {
            lock (mutex)
            {
                if (repositories == null)
                {
                    repositories = RepositoryFactory.GetRepositories<T>();
                }
            }
            return (T)repositories[typeof(T).Name];
        }

    }

    #endregion

    #region Context

    public class ContextEF : IDisposable
    {
        public bool Commit()
        {
            return true;
        }

        public void Dispose()
        {
            //implement the dispose here
        }
    }

    #endregion

    #region Repositories

    public interface IRepositoryBase
    {

    }

    public interface IRepository<TEntity> : IRepositoryBase where TEntity : Entity
    {
        TEntity Create(TEntity entity);

        TEntity Update(TEntity entity);

        void Delete(Expression<Func<TEntity, bool>> predicate);

        TEntity FindOne(Expression<Func<TEntity, bool>> predicate);

        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);

        IEnumerable<TEntity> FindAll();

    }

    public interface IRepositorysEF
    {
        void SetContext(ContextEF context);
    }

    public abstract class RepositoryEF<TEntity> : IRepository<TEntity>, IRepositorysEF where TEntity : Entity
    {
        public void SetContext(ContextEF context)
        {
            //set the entity context here
        }
        public TEntity Create(TEntity entity)
        {
            return entity;
        }

        public void Delete(Expression<Func<TEntity, bool>> predicate)
        {

        }

        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return new List<TEntity>();
        }

        public IEnumerable<TEntity> FindAll()
        {
            return new List<TEntity>();
        }

        public TEntity FindOne(Expression<Func<TEntity, bool>> predicate)
        {
            return null;
        }

        public TEntity Update(TEntity entity)
        {
            return entity;
        }
    }

    public interface ICustomerRepository : IRepository<Customer>
    {

    }

    public interface IEmployeeRepository : IRepository<Employee>
    {

    }

    public class CustomerRepository : RepositoryEF<Customer>, ICustomerRepository
    {

    }

    public class EmployeeRepository : RepositoryEF<Employee>, IEmployeeRepository
    {

    }
    #endregion

    #region Entities

    public class Entity
    {
        public int Id { get; internal set; }
    }

    public class Customer : Entity
    {
        public string Name { get; internal set; }

    }

    public class Employee : Entity
    {

    }

    #endregion
    
}
