﻿using AutoMapper;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.ChangeTracking;
using MvcTemplate.Data.Core;
using MvcTemplate.Data.Logging;
using MvcTemplate.Objects;
using MvcTemplate.Tests.Data;
using MvcTemplate.Tests.Objects;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MvcTemplate.Tests.Unit.Data.Core
{
    public class UnitOfWorkTests : IDisposable
    {
        private TestingContext context;
        private UnitOfWork unitOfWork;
        private IAuditLogger logger;

        public UnitOfWorkTests()
        {
            context = new TestingContext();
            logger = Substitute.For<IAuditLogger>();
            unitOfWork = new UnitOfWork(context, logger);

            context.Set<TestModel>().RemoveRange(context.Set<TestModel>());
            context.SaveChanges();
        }
        public void Dispose()
        {
            unitOfWork.Dispose();
            context.Dispose();
        }

        #region Method: GetAs<TModel, TDestination>(String id)

        [Fact]
        public void GetAs_ReturnsModelAsDestinationModelById()
        {
            TestModel model = ObjectFactory.CreateTestModel();
            context.Set<TestModel>().Add(model);
            context.SaveChanges();

            TestView expected = Mapper.Map<TestView>(context.Set<TestModel>().AsNoTracking().Single());
            TestView actual = unitOfWork.GetAs<TestModel, TestView>(model.Id);

            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.Text, actual.Text);
            Assert.Equal(expected.Id, actual.Id);
        }

        #endregion

        #region Method: Get<TModel>(String id)

        [Fact]
        public void Get_GetsModelById()
        {
            TestModel model = ObjectFactory.CreateTestModel();
            context.Set<TestModel>().Add(model);
            context.SaveChanges();

            TestModel expected = context.Set<TestModel>().AsNoTracking().Single();
            TestModel actual = unitOfWork.Get<TestModel>(model.Id);

            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.Text, actual.Text);
            Assert.Equal(expected.Id, actual.Id);
        }

        [Fact]
        public void Get_OnModelNotFoundReturnsNull()
        {
            Assert.Null(unitOfWork.Get<TestModel>(""));
        }

        #endregion

        #region Method: To<TDestination>(Object source)

        [Fact]
        public void ToDestination_ConvertsModelToDestinationModel()
        {
            TestModel model = ObjectFactory.CreateTestModel();

            TestView actual = unitOfWork.To<TestView>(model);
            TestView expected = Mapper.Map<TestView>(model);

            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.Text, actual.Text);
            Assert.Equal(expected.Id, actual.Id);
        }

        #endregion

        #region Method: Select<TModel>()

        [Fact]
        public void Select_CreatesSelectForSet()
        {
            TestModel model = ObjectFactory.CreateTestModel();
            context.Set<TestModel>().Add(model);
            context.SaveChanges();

            IEnumerable<TestModel> actual = unitOfWork.Select<TestModel>();
            IEnumerable<TestModel> expected = context.Set<TestModel>();

            Assert.Equal(expected, actual);
        }

        #endregion

        #region Method: Insert<TModel>(TModel model)

        [Fact]
        public void Insert_AddsModelToDbSet()
        {
            TestModel model = ObjectFactory.CreateTestModel();
            unitOfWork.Insert(model);

            Object actual = context.ChangeTracker.Entries<TestModel>().Single().Entity;
            Object expected = model;

            Assert.Equal(EntityState.Added, context.Entry(model).State);
            Assert.Same(expected, actual);
        }

        #endregion

        #region Method: Update(TModel model)

        [Fact]
        public void Update_UpdatesModel()
        {
            TestModel model = ObjectFactory.CreateTestModel();
            model.Text += "Test";

            unitOfWork.Update(model);

            EntityEntry<TestModel> actual = context.Entry(model);
            TestModel expected = model;

            Assert.Equal(expected.CreationDate, actual.Entity.CreationDate);
            Assert.Equal(EntityState.Modified, actual.State);
            Assert.Equal(expected.Text, actual.Entity.Text);
            Assert.Equal(expected.Id, actual.Entity.Id);
        }

        [Fact]
        public void Update_DoesNotModifyCreationDate()
        {
            TestModel model = ObjectFactory.CreateTestModel();

            unitOfWork.Update(model);

            Assert.False(context.Entry(model).Property(prop => prop.CreationDate).IsModified);
        }

        #endregion

        #region Method: Delete(TModel model)

        [Fact]
        public void Delete_DeletesModel()
        {
            TestModel model = ObjectFactory.CreateTestModel();
            context.Set<TestModel>().Add(model);
            context.SaveChanges();

            unitOfWork.Delete(model);
            context.SaveChanges();

            Assert.Empty(context.Set<TestModel>());
        }

        #endregion

        #region Method: Delete(String id)

        [Fact]
        public void Delete_DeletesModelById()
        {
            TestModel model = ObjectFactory.CreateTestModel();
            context.Set<TestModel>().Add(model);
            context.SaveChanges();

            unitOfWork.Delete<TestModel>(model.Id);
            context.SaveChanges();

            Assert.Empty(context.Set<TestModel>());
        }

        #endregion

        #region Method: Rollback()

        [Fact]
        public void RollBack_RollbacksChanges()
        {
            context.Set<TestModel>().Add(ObjectFactory.CreateTestModel());

            unitOfWork.Rollback();
            unitOfWork.Commit();

            Assert.Empty(unitOfWork.Select<TestModel>());
        }

        #endregion

        #region Method: Commit()

        [Fact]
        public void Commit_SavesChanges()
        {
            TestingContext context = Substitute.For<TestingContext>();
            UnitOfWork unitOfWork = new UnitOfWork(context);

            unitOfWork.Commit();

            context.Received().SaveChanges();
        }

        [Fact]
        public void Commit_LogsEntities()
        {
            unitOfWork.Commit();

            logger.Received().Log(Arg.Any<IEnumerable<EntityEntry<BaseModel>>>());
            logger.Received().Save();
        }

        [Fact]
        public void Commit_DoesNotSaveLogsOnFailedCommit()
        {
            unitOfWork.Insert(new TestModel { Text = new String('X', 513) });
            Exception exception = Record.Exception(() => unitOfWork.Commit());

            logger.Received().Log(Arg.Any<IEnumerable<EntityEntry<BaseModel>>>());
            logger.DidNotReceive().Save();
            Assert.NotNull(exception);
        }

        #endregion

        #region Method: Dispose()

        [Fact]
        public void Dispose_DiposesLogger()
        {
            unitOfWork.Dispose();

            logger.Received().Dispose();
        }

        [Fact]
        public void Dispose_DiposesContext()
        {
            TestingContext context = Substitute.For<TestingContext>();
            UnitOfWork unitOfWork = new UnitOfWork(context);

            unitOfWork.Dispose();

            context.Received().Dispose();
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            unitOfWork.Dispose();
            unitOfWork.Dispose();
        }

        #endregion
    }
}
