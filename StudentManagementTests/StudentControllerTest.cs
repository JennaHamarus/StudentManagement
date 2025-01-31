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

            //Ei lisätä dataa, jotta opiskelijaa ei löydy
            using (var context = new StudentContext(options))
            {
                var controller = new StudentsController(context);
                var result = await controller.GetStudent(99); //99 puuttuu

                Assert.IsType<NotFoundResult>(result.Result);
            }
        }
    }
}