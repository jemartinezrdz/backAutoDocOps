using AutoMapper;
using AutoDocOps.Tests.Common;

namespace AutoDocOps.Tests.Common;

public class TestProfile : Profile
{
    public TestProfile()
    {
        // Mapping for testing AutoMapper functionality
        CreateMap<TestProject, TestProjectDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC")));
    }
}
