﻿using Microsoft.AspNetCore.Mvc;
using Moq;
using ParkingZoneApp.Areas.Admin.Controllers;
using ParkingZoneApp.Enums;
using ParkingZoneApp.Models;
using ParkingZoneApp.Services;
using ParkingZoneApp.Services.ParkingSlotService;
using ParkingZoneApp.ViewModels.ParkingSlotViewModels;
using System.Text.Json;

namespace ParkingSlots.Tests.Controller;

public class ParkingSlotControllerTests
{
    private readonly Mock<IParkingSlotService> _slotService;
    private readonly Mock<IParkingZoneService> _zoneService;
    private readonly ParkingSlotController _controller;
    private readonly ParkingZone _parkingZone;
    private readonly List<ParkingSlot> _parkingSlot;
    private readonly int _id = 1;

    public ParkingSlotControllerTests()
    {
        _slotService = new Mock<IParkingSlotService>();
        _zoneService = new Mock<IParkingZoneService>();
        _controller = new ParkingSlotController(_slotService.Object, _zoneService.Object);
        _parkingZone = new () { Id = _id, Name = "Zone 1" };
        _parkingSlot = new List<ParkingSlot>
        {
            new ()
            {
                Id = 1,
                Number = 2,
                IsAvailableForBooking = false,
                ParkingZoneId = 1,
                Category = 0,
            },
            new ()
            {
                Id = 2,
                Number = 3,
                IsAvailableForBooking = true,
                ParkingZoneId = 1,
                Category = SlotCategory.Premium
            }
        }; 
    }

    #region Index
    [Fact]
    public void GivenParkingZoneId_WhenIndexIsCalled_ThenReturnsParkingSlotsVM()
    {
        // Arrange
        var expectedVMs = new List<ListOfParkingSlotsVM>();
        expectedVMs.AddRange(ListOfParkingSlotsVM.MapToModel(_parkingSlot));

        _slotService.Setup(x => x.GetSlotsByZoneId(_id)).Returns(_parkingSlot);
        _zoneService.Setup(x => x.GetById(_id)).Returns(_parkingZone);

        //Act
        var result = _controller.Index(_id);
        var model = ((ViewResult)result).Model;

        //Assert
        Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<IEnumerable<ListOfParkingSlotsVM>>(model);
        Assert.Equal(JsonSerializer.Serialize(model), JsonSerializer.Serialize(expectedVMs));
        Assert.NotNull(result);
        Assert.NotNull(model);
        _slotService.Verify(_parkingSlot => _parkingSlot.GetSlotsByZoneId(_id), Times.Once);
    }
    #endregion

    #region Create
    [Fact]
    public void GivenParkingZoneId_WhenCreateIsCalled_ThenReturnsCreateViewModel()
    {
        //Arrange
        var createVM = new CreateViewModel() { ParkingZoneId = _id };

        //Act
        var result = _controller.Create(_id);
        var model = ((ViewResult)result).Model;

        //Assert
        Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<CreateViewModel>(model);
        Assert.Equal(JsonSerializer.Serialize(model), JsonSerializer.Serialize(createVM));
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenCreateModel_WhenCreateIsCalled_ThenSlotExistsAndReturnsViewResult()
    {
        //Arrange
        var createVM = new CreateViewModel() { ParkingZoneId = _id, Number = 2 };
        _controller.ModelState.AddModelError("Number", "Slot number already exists in this zone");
        _slotService.Setup(p => p.IsExistingParkingSlot(createVM.ParkingZoneId, createVM.Number))
                    .Returns(true);

        //Act
        var result = _controller.Create(new CreateViewModel());

        //Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenCreateModel_WhenCreateIsCalled_ThenSlotNumberIsNegativeAndReturnsViewResult()
    {
        //Arrange
        _controller.ModelState.AddModelError("Number", "Number can not be negative");
        CreateViewModel createModel = new() { ParkingZoneId = 1, Number = -5 };
        _slotService.Setup(s => s.IsExistingParkingSlot(createModel.ParkingZoneId, createModel.Number))
                    .Returns(true);

        //Act
        var result = _controller.Create(new CreateViewModel());

        //Assert
        Assert.IsType<ViewResult>(result);
        Assert.IsType<CreateViewModel>(createModel);
        Assert.False(_controller.ModelState.IsValid);
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenCreateModel_WhenCreateIsCalled_ThenModelStateIsTrueAndReturnsRedirectToActionResult()
    {
        //Arrange
        var createVM = new CreateViewModel() { ParkingZoneId = 3, Number = 2 };
        _slotService.Setup(p => p.IsExistingParkingSlot(createVM.ParkingZoneId, createVM.Number))
                    .Returns(false);
        _slotService.Setup(p => p.Insert(It.IsAny<ParkingSlot>()));

        //Act
        var result = _controller.Create(createVM);
        var model = result as RedirectToActionResult;

        //Assert
        Assert.Equal("Index", model.ActionName);
        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(_controller.ModelState.IsValid);
        Assert.NotNull(result);
        _slotService.Verify(p => p.Insert(It.IsAny<ParkingSlot>()), Times.Once);
        _slotService.Verify(p => p.IsExistingParkingSlot(createVM.ParkingZoneId, createVM.Number), Times.Once);
    }
    #endregion

    #region Details
    [Fact]
    public void GivenId_WhenDetailsIsCalled_ThenReturnsNotFound()
    {
        //Arrange
        _slotService.Setup(s => s.GetById(_id)).Returns(() => null);

        //Act
        var result = _controller.Details(_id);

        //Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        _slotService.Verify(s => s.GetById(_id), Times.Once);
 
    }

    [Fact]
    public void GivenId_WhenDetailsIsCalled_ThenReturnsDetailsViewModel()
    {
        //Arrange
        _slotService.Setup(s => s.GetById(_id)).Returns(_parkingSlot[0]);
        var expectdVM = new DetailsViewModel(_parkingSlot[0]);

        //Act
        var result = _controller.Details(_id);
        var model = ((ViewResult)result).Model;

        //Assert
        Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<DetailsViewModel>(model);
        Assert.Equal(JsonSerializer.Serialize(model), JsonSerializer.Serialize(expectdVM));
        Assert.NotNull(result);
        Assert.NotNull(model);
        _slotService.Verify(s => s.GetById(_id), Times.Once);
    }
    #endregion
}
