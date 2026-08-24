using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SpaceBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingEmailNotificationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "facilities",
                columns: table => new
                {
                    facilityid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    facilityname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facilities", x => x.facilityid);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    locationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    locationname = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.locationid);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    roleid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rolename = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.roleid);
                });

            migrationBuilder.CreateTable(
                name: "roomtypes",
                columns: table => new
                {
                    roomtypeid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    typename = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roomtypes", x => x.roomtypeid);
                });

            migrationBuilder.CreateTable(
                name: "offices",
                columns: table => new
                {
                    officeid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    locationid = table.Column<int>(type: "integer", nullable: false),
                    officename = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offices", x => x.officeid);
                    table.ForeignKey(
                        name: "FK_offices_locations_locationid",
                        column: x => x.locationid,
                        principalTable: "locations",
                        principalColumn: "locationid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    employeeid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    roleid = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    passwordhash = table.Column<string>(type: "text", nullable: false),
                    department = table.Column<string>(type: "text", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false),
                    createdon = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.employeeid);
                    table.ForeignKey(
                        name: "FK_employees_roles_roleid",
                        column: x => x.roleid,
                        principalTable: "roles",
                        principalColumn: "roleid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "modules",
                columns: table => new
                {
                    moduleid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    officeid = table.Column<int>(type: "integer", nullable: false),
                    modulename = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recordingestedby = table.Column<string>(type: "text", nullable: true),
                    recordingestedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recordmodifiedby = table.Column<string>(type: "text", nullable: true),
                    recordmodifiedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modules", x => x.moduleid);
                    table.ForeignKey(
                        name: "FK_modules_offices_officeid",
                        column: x => x.officeid,
                        principalTable: "offices",
                        principalColumn: "officeid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    roomid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    roomnumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    roomtypeid = table.Column<int>(type: "integer", nullable: false),
                    moduleid = table.Column<int>(type: "integer", nullable: false),
                    roomname = table.Column<string>(type: "text", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    isblocked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.roomid);
                    table.ForeignKey(
                        name: "FK_rooms_modules_moduleid",
                        column: x => x.moduleid,
                        principalTable: "modules",
                        principalColumn: "moduleid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rooms_roomtypes_roomtypeid",
                        column: x => x.roomtypeid,
                        principalTable: "roomtypes",
                        principalColumn: "roomtypeid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "seats",
                columns: table => new
                {
                    seatid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    moduleid = table.Column<int>(type: "integer", nullable: false),
                    section = table.Column<string>(type: "text", nullable: true),
                    seatnumber = table.Column<string>(type: "text", nullable: false),
                    rownumber = table.Column<string>(type: "text", nullable: false),
                    columnnumber = table.Column<int>(type: "integer", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seats", x => x.seatid);
                    table.ForeignKey(
                        name: "FK_seats_modules_moduleid",
                        column: x => x.moduleid,
                        principalTable: "modules",
                        principalColumn: "moduleid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    bookingid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    roomid = table.Column<int>(type: "integer", nullable: false),
                    employeeid = table.Column<int>(type: "integer", nullable: false),
                    meetingtitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    participantcount = table.Column<int>(type: "integer", nullable: false),
                    bookingdate = table.Column<DateOnly>(type: "date", nullable: false),
                    starttime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    endtime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    bookedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cancellationreason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    startremindersent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    endremindersent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.bookingid);
                    table.ForeignKey(
                        name: "FK_bookings_employees_employeeid",
                        column: x => x.employeeid,
                        principalTable: "employees",
                        principalColumn: "employeeid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bookings_rooms_roomid",
                        column: x => x.roomid,
                        principalTable: "rooms",
                        principalColumn: "roomid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roomfacilities",
                columns: table => new
                {
                    roomfacilityid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    roomid = table.Column<int>(type: "integer", nullable: false),
                    facilityid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roomfacilities", x => x.roomfacilityid);
                    table.ForeignKey(
                        name: "FK_roomfacilities_facilities_facilityid",
                        column: x => x.facilityid,
                        principalTable: "facilities",
                        principalColumn: "facilityid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_roomfacilities_rooms_roomid",
                        column: x => x.roomid,
                        principalTable: "rooms",
                        principalColumn: "roomid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotseatbookings",
                columns: table => new
                {
                    hotseatbookingid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    seatid = table.Column<int>(type: "integer", nullable: false),
                    employeeid = table.Column<int>(type: "integer", nullable: false),
                    bookingdate = table.Column<DateOnly>(type: "date", nullable: false),
                    bookingstatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bookedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    checkindeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    checkintime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    releasedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recordingestedby = table.Column<string>(type: "text", nullable: true),
                    recordingestedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recordmodifiedby = table.Column<string>(type: "text", nullable: true),
                    recordmodifiedon = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotseatbookings", x => x.hotseatbookingid);
                    table.ForeignKey(
                        name: "FK_hotseatbookings_employees_employeeid",
                        column: x => x.employeeid,
                        principalTable: "employees",
                        principalColumn: "employeeid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hotseatbookings_seats_seatid",
                        column: x => x.seatid,
                        principalTable: "seats",
                        principalColumn: "seatid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bookingemailnotifications",
                columns: table => new
                {
                    bookingemailnotificationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bookingid = table.Column<int>(type: "integer", nullable: false),
                    notificationtype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sentat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookingemailnotifications", x => x.bookingemailnotificationid);
                    table.ForeignKey(
                        name: "FK_bookingemailnotifications_bookings_bookingid",
                        column: x => x.bookingid,
                        principalTable: "bookings",
                        principalColumn: "bookingid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checkins",
                columns: table => new
                {
                    checkinid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bookingid = table.Column<int>(type: "integer", nullable: false),
                    checkedinat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkins", x => x.checkinid);
                    table.ForeignKey(
                        name: "FK_checkins_bookings_bookingid",
                        column: x => x.bookingid,
                        principalTable: "bookings",
                        principalColumn: "bookingid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notificationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employeeid = table.Column<int>(type: "integer", nullable: false),
                    bookingid = table.Column<int>(type: "integer", nullable: true),
                    hotseatbookingid = table.Column<int>(type: "integer", nullable: true),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    isread = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.notificationid);
                    table.ForeignKey(
                        name: "FK_notifications_bookings_bookingid",
                        column: x => x.bookingid,
                        principalTable: "bookings",
                        principalColumn: "bookingid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_notifications_employees_employeeid",
                        column: x => x.employeeid,
                        principalTable: "employees",
                        principalColumn: "employeeid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notifications_hotseatbookings_hotseatbookingid",
                        column: x => x.hotseatbookingid,
                        principalTable: "hotseatbookings",
                        principalColumn: "hotseatbookingid",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookingemailnotifications_bookingid_notificationtype",
                table: "bookingemailnotifications",
                columns: new[] { "bookingid", "notificationtype" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_employeeid",
                table: "bookings",
                column: "employeeid");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_roomid",
                table: "bookings",
                column: "roomid");

            migrationBuilder.CreateIndex(
                name: "IX_checkins_bookingid",
                table: "checkins",
                column: "bookingid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_roleid",
                table: "employees",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "IX_hotseatbookings_employeeid_bookingdate",
                table: "hotseatbookings",
                columns: new[] { "employeeid", "bookingdate" });

            migrationBuilder.CreateIndex(
                name: "IX_hotseatbookings_seatid_bookingdate",
                table: "hotseatbookings",
                columns: new[] { "seatid", "bookingdate" });

            migrationBuilder.CreateIndex(
                name: "IX_modules_officeid",
                table: "modules",
                column: "officeid");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_bookingid",
                table: "notifications",
                column: "bookingid");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_createdat",
                table: "notifications",
                column: "createdat");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_employeeid",
                table: "notifications",
                column: "employeeid");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_hotseatbookingid",
                table: "notifications",
                column: "hotseatbookingid");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_isread",
                table: "notifications",
                column: "isread");

            migrationBuilder.CreateIndex(
                name: "IX_offices_locationid",
                table: "offices",
                column: "locationid");

            migrationBuilder.CreateIndex(
                name: "IX_roomfacilities_facilityid",
                table: "roomfacilities",
                column: "facilityid");

            migrationBuilder.CreateIndex(
                name: "IX_roomfacilities_roomid",
                table: "roomfacilities",
                column: "roomid");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_moduleid",
                table: "rooms",
                column: "moduleid");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_roomtypeid",
                table: "rooms",
                column: "roomtypeid");

            migrationBuilder.CreateIndex(
                name: "IX_seats_moduleid",
                table: "seats",
                column: "moduleid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookingemailnotifications");

            migrationBuilder.DropTable(
                name: "checkins");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "roomfacilities");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "hotseatbookings");

            migrationBuilder.DropTable(
                name: "facilities");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "seats");

            migrationBuilder.DropTable(
                name: "roomtypes");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "modules");

            migrationBuilder.DropTable(
                name: "offices");

            migrationBuilder.DropTable(
                name: "locations");
        }
    }
}
