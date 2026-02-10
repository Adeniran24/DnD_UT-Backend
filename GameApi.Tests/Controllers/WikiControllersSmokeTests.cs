using GameApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using GameApi.Tests.TestUtilities;
using System.IO;
using DnDAPI.Controllers;
using DndFeaturesApp.Controllers;
using DndSubclasses.Controllers;
using DndSubraces.Controllers;

namespace GameApi.Tests.Controllers;

public class WikiControllersSmokeTests
{
    [Fact]
    public void AbilityScores_GetAll_ReturnsOk_WhenDataLoaded()
    {
        var controller = new AbilityScoresController();
        var result = controller.GetAll();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Alignments_GetAll_ReturnsOk_WhenDataLoaded()
    {
        var controller = new AlignmentsController();
        var result = controller.GetAll();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Backgrounds_GetAll_ReturnsOk_WhenDataLoaded()
    {
        var controller = new BackgroundsController();
        var result = controller.GetAll();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Classes_GetAll_ReturnsContent_WhenFileAvailable()
    {
        var controller = new ClassesController();
        var result = await controller.GetAll();
        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("application/json", content.ContentType);
    }

    [Fact]
    public async Task Conditions_GetAll_ReturnsOk_WhenDataLoaded()
    {
        var controller = new ConditionsController();
        var result = await controller.GetAll();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void DamageTypes_GetAll_ReturnsOk_WhenDataLoaded()
    {
        var controller = new DamageTypesController();
        var result = controller.GetAll();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Equipment_GetAll_ReturnsOk_WhenDataLoaded()
    {
        var controller = new EquipmentController();
        var result = controller.GetAll();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Languages_Controller_Throws_WhenFileMissing()
    {
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Assert.Throws<InvalidOperationException>(() =>
            new LanguagesController(NullLogger<LanguagesController>.Instance, env));
    }

    [Fact]
    public void MagicItems_GetAll_ReturnsOk_WhenEmpty()
    {
        var controller = new MagicItemsController(NullLogger<MagicItemsController>.Instance);
        var result = controller.GetAllMagicItems();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void MagicSchools_GetAll_Returns500_WhenFileMissing()
    {
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var controller = new MagicSchoolsController(NullLogger<MagicSchoolsController>.Instance, env);
        var result = controller.GetAll();
        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public void Monsters_GetAll_ReturnsOk_WhenEmpty()
    {
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var controller = new MonstersController(NullLogger<MonstersController>.Instance, env);
        var result = controller.GetAll();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Proficiencies_GetAll_ReturnsOk_WhenEmpty()
    {
        var controller = new ProficienciesController(NullLogger<ProficienciesController>.Instance);
        var result = controller.GetAllProficiencies();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void Races_GetAll_ReturnsOk_WhenEmpty()
    {
        var controller = new RacesController(NullLogger<RacesController>.Instance);
        var result = controller.GetAllRaces();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void Skills_GetAll_ReturnsOk_WhenLoaded()
    {
        var controller = new SkillsController(NullLogger<SkillsController>.Instance);
        var result = controller.GetAllSkills();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void Spells_GetAll_ReturnsOk_WhenEmpty()
    {
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var controller = new SpellsController(NullLogger<SpellsController>.Instance, env);
        var result = controller.GetAll();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void Subclasses_GetAll_ReturnsOk_WhenSeeded()
    {
        var controller = new SubclassesController();
        var result = controller.GetSubclasses();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void Subraces_GetAll_ReturnsOk_WhenSeeded()
    {
        var controller = new SubracesController();
        var result = controller.GetSubraces();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task WeaponProperties_Get_ReturnsNotFound_WhenFileMissing()
    {
        var env = TestHelper.CreateWebHostEnvironment();
        env.ContentRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var controller = new WeaponPropertiesController(env);
        var result = await controller.Get();
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
