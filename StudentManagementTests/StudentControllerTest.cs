using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using StudentManagement.Data;
using StudentManagement.Models;
using StudentManagement.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace StudentManagementTests
{
    public class StudentControllerTests
    {
        [Fact]
        public async Task GetStudents_ReturnAllStudents()
        {
            //1.Luodaan DbContextOptions in-memory -tietokantaa varten
            var options = new DbContextOptionsBuilder<StudentContext>()
                .UseInMemoryDatabase(databaseName: "StudentDbTest")
                .Options;
        
            // 2. Lisätään "testidataan" muutama opiskelija
            using (var context = new StudentContext(options))
            {
                context.Students.Add(new Student {Id = 1, FirstName = "Maija", LastName = "Meikäläinen", Age = 20});
                context.Students.Add(new Student {Id = 2, FirstName = "Matti", LastName = "Meikäläinen", Age = 22});
                await context.SaveChangesAsync();
            }

            //3. Suoritetaan varsinainen testi uudella Context-instanssilla
            using (var context = new StudentContext(options))
            {
                var controller = new StudentsController(context);

                var result = await controller.GetStudents();

                //4.Varmistetaan, että tuloksena on kaikki oppilaat (2kpl)
                var actionResult = Assert.IsType<ActionResult<IEnumerable<Student>>>(result);
                var studentList = Assert.IsAssignableFrom<IEnumerable<Student>>(actionResult.Value);

                Assert.Equal(2, studentList.Count());
            }
        }

        [Fact]
        public async Task GetStudent_ReturnsNotFound_WhenStudentDoesNotExist()
        {
            //InMemory -kanta luodaan
            var options = new DbContextOptionsBuilder<StudentContext>()
                .UseInMemoryDatabase(databaseName: "StudentDbTest_NotFound")
                .Options; 

            //Ei lisätä dataa, jos opiskelijaa ei löydy
            using (var context = new StudentContext(options))
            {
                var controller = new StudentsController(context);
                var result = await controller.GetStudent(99); //99 puuttuu

                Assert.IsType<NotFoundResult>(result.Result);
            }
        }

        [Fact]
        public async Task PostStudent_ReturnsCreatedAtAction_WhenStudentIsAdded()
        {
            //InMemory -kanta luodaan
            var options = new DbContextOptionsBuilder<StudentContext>()
                .UseInMemoryDatabase(databaseName: "StudentDbTest_Post")
                .Options;

            // Luodaan uusi opiskelija muuttujaan
            var newStudent = new Student {FirstName = "Erkki", LastName = "Pekkanen", Age = 45};

            //Suoritetaan testi
            using (var context = new StudentContext(options))
            {
                //Luodaan StudentsController instanssi
                var controller = new StudentsController(context);
                //Annetaan PostStudent metodille syöte, joka tehtiin aiemmin
                var result = await controller.PostStudent(newStudent);

                //Onko tulos CreatedAtAction
                var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
                //Varmistetaan, ettei RouteValue ole NULL
                 Assert.NotNull(createdAtActionResult.RouteValues);
                //Varmistetaan, että reitissä on Id
                 Assert.True(createdAtActionResult.RouteValues.ContainsKey("id"),"RouteValues ei sisällä 'id' avainta");

                 var id = createdAtActionResult.RouteValues["id"];
                 Assert.NotNull(id);
                 //Onko id sama kuin odotettu arvo
                 Assert.Equal(newStudent.Id, id);
            }
            //Onko opiskelija lisätty tietokantaan
            using (var contextCheck = new StudentContext(options))
            {
                //Haetaan opiskelija id:llä
                var StudentInDb = await contextCheck.Students.FindAsync(newStudent.Id);
                //Löytyykö opiskelija tietokannasta
                Assert.NotNull(StudentInDb);
                //Onko opiskelijan etunimi oikein
                Assert.Equal("Erkki", StudentInDb.FirstName);
                //Onko Sukunimi oikein
                Assert.Equal("Pekkanen", StudentInDb.LastName);
                //Onko ikä oikein
                Assert.Equal(45, StudentInDb.Age);
            }
        }
        [Fact]
        public async Task PutStudent_StudentInfoUpdated()
        {
            //InMemory -kanta luodaan
            var options = new DbContextOptionsBuilder<StudentContext>()
                .UseInMemoryDatabase(databaseName: "StudentDbTest_Put")
                .Options;

            var newStudent = new Student {FirstName = "Erkki", LastName = "Pekkanen", Age = 45};

            using (var context = new StudentContext(options))
            {
                //Luodaan StudentsController instanssi
                var controller = new StudentsController(context);
                //Luodaan tietokantaan opiskelija
                var studentInfo = await controller.PostStudent(newStudent);
                var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(studentInfo.Result);
                var createdStudent = Assert.IsType<Student>(createdAtActionResult.Value);

                //Haetaan juuri lisätty opiskelija tietokannasta
                var studentInDb = await context.Students.FindAsync(createdStudent.Id);
                Assert.NotNull(studentInDb);

                //Muutetaan opiskelijan tiedot
                studentInDb.FirstName = "Jukka";
                studentInDb.LastName = "Kukkanen";
                studentInDb.Age = 38;
                
                //Annetaan PostStudent metodille opiskelijan uudet tiedot
                var result = await controller.PutStudent(studentInDb.Id, studentInDb);
                //Tarkistetaan onnistuiko päivitys
                Assert.IsType<NoContentResult>(result);

                //Tarkistetaan onko tiedot päivittynyt tietokantaan
                var updatedStudent = await context.Students.FindAsync(studentInDb.Id);
                Assert.NotNull(updatedStudent);
                Assert.Equal("Jukka", updatedStudent.FirstName);
                Assert.Equal("Kukkanen", updatedStudent.LastName);
                Assert.Equal(38, updatedStudent.Age);
            }
        }
    }
}