namespace EnumTest
{
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using FluentNHibernate.Mapping;
    using NHibernate.Cfg;
    using NHibernate.Tool.hbm2ddl;
    using Other;

    internal class Program
    {
        private static void Main ()
        {
            WithoutFNH ();
            WithFNH ();
        }

        private static void WithoutFNH ()
        {
            var cfg = new Configuration ();
            cfg.AddAssembly (typeof (Program).Assembly);
            Test (cfg);
        }

        private static void WithFNH ()
        {
            var cfg = Fluently.Configure ()
                .Database (SQLiteConfiguration.Standard.InMemory)
                .Mappings (m => m.FluentMappings.AddFromAssemblyOf<Program> ())
                .BuildConfiguration ();

            Test (cfg);
        }

        private static void Test (Configuration cfg)
        {
            var sf = cfg.BuildSessionFactory ();

            using (var s = sf.OpenSession ())
            using (var tx = s.BeginTransaction ()) {
                var export = new SchemaExport (cfg);
                export.Execute (false, true, false, s.Connection, null);

                var foo = new Foo {Bar = Bar.Y};
                s.Save (foo);

                s.Flush ();
                s.Clear ();

                var foos = s.CreateQuery ("FROM Foo WHERE Bar = Bar.Y").List<Foo> ();

                tx.Commit ();
            }
        }
    }

    public class FooMapping : ClassMap<Foo>
    {
        public FooMapping ()
        {
            Id (x => x.Id).GeneratedBy.HiLo ("100");

            // This works
            // Map (x => x.Bar).CustomType ("");

            // This does not work
            Map (x => x.Bar);

            // The key difference between the 2 mappings is FNH uses:
            //     FluentNHibernate.Mapping.GenericEnumMapper<EnumTest.Other.Bar>
            // by default when mapping enums. This seems to interfere with the import and using
            // the aliased type.

            ImportType<Bar> ();
        }
    }

    public class Foo
    {
        public virtual long Id { get; set; }
        public virtual Bar Bar { get; set; }
    }

    namespace Other
    {
        public enum Bar
        {
            X,
            Y,
        }
    }
}
