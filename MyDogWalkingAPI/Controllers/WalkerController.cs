﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyDogWalkingAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MyDogWalkingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalkerController : ControllerBase
    {
        private readonly IConfiguration _config;
        public WalkerController(IConfiguration config)
        {
            _config = config;
        }
        //COMPUTED PROPERTY FOR THE CONNECTION
        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        public int Id { get; private set; }
        public int WalkerId { get; private set; }

        //GET ALL

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? neighborhoodId)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Name, NeighborhoodId FROM Walker";

                    if (neighborhoodId != null)
                    {
                        cmd.CommandText += " WHERE neighborhoodId = @neighborhoodId";
                        cmd.Parameters.Add(new SqlParameter("@neighborhoodId", neighborhoodId));
                    }

                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Walker> walkers = new List<Walker>();

                    while (reader.Read())
                    {
                        Walker walker = new Walker
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId"))
                        };

                        walkers.Add(walker);
                    }
                    reader.Close();

                    return Ok(walkers);
                }
            }
        }

        //GET BY ID
        [HttpGet("{id}", Name = "GetWalker")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                        w.Id AS WalkerId, w.Name, w.NeighborhoodId, wk.Id AS WalkId, wk.WalkerId AS WalkWalkerId, wk.Duration AS WalkDuration, wk.DogId, wk.Date AS WalkDate
                        FROM Walker w
                        LEFT JOIN Walks wk
                        ON w.Id = wk.WalkerId
                        WHERE w.Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Walker walker = null;

                    while (reader.Read())
                    {
                       if (walker == null)
                        {
                            walker = new Walker
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("WalkerId")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                Walks = new List<Walk>()
                            };
                        }
                        walker.Walks.Add(new Walk()
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("WalkId")),
                            Date = reader.GetDateTime(reader.GetOrdinal("WalkDate")),
                            WalkerId = reader.GetInt32(reader.GetOrdinal("WalkWalkerId")),
                            Duration = reader.GetInt32(reader.GetOrdinal("WalkDuration")),
                            DogId = reader.GetInt32(reader.GetOrdinal("DogId")),
                        });
                        
                    }
                    reader.Close();

                    return Ok(walker);
                }
            }
        }

        //POST
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Walker walker)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Walker (Name, NeighborhoodId)
                                        OUTPUT INSERTED.Id
                                        VALUES (@name, @neighborhoodId)";
                    cmd.Parameters.Add(new SqlParameter("@name", walker.Name));
                    cmd.Parameters.Add(new SqlParameter("@neighborhoodId", walker.NeighborhoodId));

                    int newId = (int)cmd.ExecuteScalar();
                    walker.Id = newId;
                    return CreatedAtRoute("GetWalker", new { id = newId }, walker);
                }
            }
        }
        //PUT

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Walker walker)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Walker
                                            SET Name = @name,
                                            NeighborhoodId = @neighborhoodId
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@name", walker.Name));
                        cmd.Parameters.Add(new SqlParameter("@neighborhoodId", walker.NeighborhoodId));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!WalkerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        //Delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Walker WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!WalkerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
        //Check method
        private bool WalkerExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Name, NeighborhoodId
                        FROM Walker
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }

    }
}
