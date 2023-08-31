using AutoMapper;
using blog_main.Dtos;
using blog_main.Models;

namespace blog_main.Profiles
{
    public class AppMappingProfile : Profile 
    {
        public AppMappingProfile()
        {
            CreateMap<PostCreateDto, Post>();
        }
    }
}