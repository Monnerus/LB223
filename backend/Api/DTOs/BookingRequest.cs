using Core.Enums;

namespace Api.DTOs;

public record BookingRequest(BookingType Type, int Amount);
