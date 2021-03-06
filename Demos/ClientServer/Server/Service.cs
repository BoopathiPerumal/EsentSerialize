﻿using Database;
using EsentSerialization;
using EsentSerialization.Linq;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Server
{
	[ServiceBehavior( InstanceContextMode = InstanceContextMode.Single )]
	class Service : iPersonsService
	{
		readonly SessionPool sessionPool;
		readonly Query<Person> orderBySex;
		readonly Query<Person> filterBySex;

		public Service( SessionPool sessionsPool )
		{
			this.sessionPool = sessionsPool;

			using( var sess = sessionPool.GetSession() )
			{
				iTypeSerializer ser = sess.serializer.FindSerializerForType( typeof( Person ) );
				orderBySex = Queries.sort<Person, Person.eSex>( ser, p => p.sex, false );
				filterBySex = Queries.filter( ser, ( Person p, Person.eSex arg ) => p.sex == arg );
			}
		}

		internal T withRecordset<T>( Func<Recordset<Person>, T> produce )
		{
			using( var sess = sessionPool.GetSession() )
			using( var trans = sess.BeginTransaction() )
			{
				return produce( sess.Recordset<Person>() );
			}
		}

		static internal PersonMessage[] array( IEnumerable<Person> enm )
		{
			return enm.Select( rec => rec.toMessage() ).ToArray();
		}

		void iPersonsService.add( PersonMessage msg )
		{
			using( var sess = sessionPool.GetSession() )
			using( var trans = sess.BeginTransaction() )
			{
				Cursor<Person> cur = sess.Cursor<Person>();
				cur.Add( msg.toRecord() );
				trans.Commit();
			}
		}

		int iPersonsService.getTotalCount()
		{
			return withRecordset( rs => rs.Count() );
		}

		PersonMessage[] iPersonsService.sortBySex()
		{
			return withRecordset( rs =>
				array( rs.all( orderBySex ) ) );
		}

		PersonMessage[] iPersonsService.queryBySex( PersonMessage.eSex value )
		{
			return withRecordset( rs =>
				array( rs.all( filterBySex, value ) ) );
		}

		PersonMessage[] iPersonsService.queryByNameSubstring( string substring )
		{
			return withRecordset( rs =>
				array( rs.where( p => p.name.Contains( substring ) ) ) );
		}
	}
}