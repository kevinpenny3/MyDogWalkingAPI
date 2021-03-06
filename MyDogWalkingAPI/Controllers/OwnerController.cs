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
    public class OwnerController : ControllerBase
    {
        private IConfiguration _config;
        public OwnerController(IConfiguration config)
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
        //GET ALL

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string include, [FromQuery] string q)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    var includeQuery = " ";
                    var joinQuery = "";
                    if (include == "neighborhood")
                    {
                        includeQuery = " n.Name NeighborhoodName, ";
                        joinQuery = " LEFT JOIN Neighborhood N ON o.NeighborhoodId = n.Id";

                    }
                    cmd.CommandText = $"SELECT o.Id, o.Name, o.Address, o.NeighborhoodId,{includeQuery}o.Phone FROM Owner o{joinQuery}";

                    if (q != null)
                    {
                        cmd.CommandText += " WHERE Name LIKE @Name";
                        cmd.Parameters.Add(new SqlParameter("@Name", "%" + q + "%"));
                    }

                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Owner> owners = new List<Owner>();
                    Owner owner = null;
                    while (reader.Read())
                    {
                        if (include == "neighborhood")
                        {
                            owner = new Owner
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                Phone = reader.GetString(reader.GetOrdinal("Phone")),
                                Neighborhood = new Neighborhood()
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                    Name = reader.GetString(reader.GetOrdinal("NeighborhoodName"))
                                }
                            };
                        }
                        else
                        {
                            owner = new Owner
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                Phone = reader.GetString(reader.GetOrdinal("Phone"))
                            };

                        }

                        owners.Add(owner);
                    }
                    reader.Close();

                    return Ok(owners);
                }
            }
        }

        //GET BY ID
        [HttpGet("{id}", Name = "GetOwner")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT o.Id, o.Name, o.Address, o.NeighborhoodId, o.Phone, n.Name NeighborhoodName, d.Id DogId ,d.Name DogName, d.Breed, d.Notes
                        FROM Owner o
                        Left Join  Neighborhood n
                        On o.NeighborhoodId = n.Id
                        Left Join Dog d
                        On o.Id = d.OwnerId
                        Where o.Id = @id";

                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Owner owner = null;

                    while (reader.Read())
                    {
                        if (owner == null)
                        {
                            owner = new Owner
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                Phone = reader.GetString(reader.GetOrdinal("Phone")),
                                NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                Neighborhood = new Neighborhood()
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                    Name = reader.GetString(reader.GetOrdinal("NeighborhoodName")),
                                },
                                Dogs = new List<Dog>()
                            };
                        }
                        owner.Dogs.Add(new Dog()
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("DogId")),
                            OwnerId = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("DogName")),
                            Breed = reader.GetString(reader.GetOrdinal("Breed")),
                            Notes = reader.GetString(reader.GetOrdinal("Notes"))
                        });
                    }
                    reader.Close();
                    return Ok(owner);
                }

            }
        }

        //POST
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Owner owner)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Owner (Name, Address, NeighborhoodId, Phone)
                                        OUTPUT INSERTED.Id
                                        VALUES (@name, @address, @neighborhoodId, @phone)";
                    cmd.Parameters.Add(new SqlParameter("@name", owner.Name));
                    cmd.Parameters.Add(new SqlParameter("@address", owner.Address));
                    cmd.Parameters.Add(new SqlParameter("@neighborhoodId", owner.NeighborhoodId));
                    cmd.Parameters.Add(new SqlParameter("@phone", owner.Phone));
                    int newId = (int)cmd.ExecuteScalar();
                    owner.Id = newId;
                    return CreatedAtRoute("GetOwner", new { id = newId }, owner);
                }
            }
        }

        //PUT

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Owner owner)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Owner
                                            SET Name = @name,
                                            Address = @address,
                                            NeighborhoodId = @neighborhoodId,
                                            Phone = @phone
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@name", owner.Name));
                        cmd.Parameters.Add(new SqlParameter("@address", owner.Address));
                        cmd.Parameters.Add(new SqlParameter("@neighborhoodId", owner.NeighborhoodId));
                        cmd.Parameters.Add(new SqlParameter("@phone", owner.Phone));
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
                if (!OwnerExists(id))
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
        private bool OwnerExists(int id)
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
